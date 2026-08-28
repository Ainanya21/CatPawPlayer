using CatPawPlayer.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class CategoryPage : Page
{
    private readonly ObservableCollection<VodItem> _items = [];
    private readonly ObservableCollection<SiteSource> _siteList = [];
    private List<CategoryItem> _categories = [];
    private Dictionary<string, List<FilterGroup>> _allFilters = [];
    private readonly Dictionary<string, string> _currentExtend = [];
    private string _activeCategoryId = "";
    private int _page = 1;
    private int _pageCount = 1;
    private bool _initialized = false;
    private bool _filterExpanded = true;
    private string? _pendingSiteKey = null;
    private double _savedScrollOffset = 0;

    private Microsoft.UI.Xaml.Controls.WebView2? _configCenterWebView;

    public CategoryPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += CategoryPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string siteKey && !string.IsNullOrEmpty(siteKey))
        {
            _pendingSiteKey = siteKey;
            _savedScrollOffset = 0;
        }

        if (e.NavigationMode == NavigationMode.Back && _initialized)
        {
            RestoreScrollPosition();
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        SaveScrollPosition();
    }

    public void SaveScrollPosition()
    {
        if (VideosScrollViewer != null)
        {
            _savedScrollOffset = VideosScrollViewer.VerticalOffset;
        }
    }

    public void RestoreScrollPosition()
    {
        if (_savedScrollOffset > 0 && VideosScrollViewer != null)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(60);
                VideosScrollViewer.ChangeView(null, _savedScrollOffset, null, true);
            });
        }
    }

    private async void CategoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            if (!string.IsNullOrEmpty(_pendingSiteKey))
            {
                var targetSite = App.Sites.FirstOrDefault(s => s.Key == _pendingSiteKey || s.Api.Contains(_pendingSiteKey));
                if (targetSite != null && targetSite.Key != App.ActiveSite?.Key)
                {
                    App.ActiveSite = targetSite;
                    var idx = App.Sites.IndexOf(targetSite);
                    if (idx >= 0) SiteSelector.SelectedIndex = idx;
                    _pendingSiteKey = null;
                    _savedScrollOffset = 0;
                    await LoadHomeDataAsync();
                    return;
                }
                _pendingSiteKey = null;
            }

            RestoreScrollPosition();
            return;
        }

        if (App.Sites.Count == 0 || App.ActiveSite == null)
        {
            await App.EnsureSitesLoadedAsync();
        }
        PopulateSiteSelector();
        ItemsGrid.ItemsSource = _items;

        if (!string.IsNullOrEmpty(_pendingSiteKey))
        {
            var targetSite = App.Sites.FirstOrDefault(s => s.Key == _pendingSiteKey || s.Api.Contains(_pendingSiteKey));
            if (targetSite != null)
            {
                App.ActiveSite = targetSite;
                var idx = App.Sites.IndexOf(targetSite);
                if (idx >= 0) SiteSelector.SelectedIndex = idx;
            }
            _pendingSiteKey = null;
        }

        await LoadHomeDataAsync();
        _initialized = true;
    }

    private void PopulateSiteSelector()
    {
        _siteList.Clear();
        foreach (var s in App.Sites) _siteList.Add(s);
        SiteSelector.ItemsSource = _siteList;
        SiteSelector.DisplayMemberPath = "CleanName";

        if (App.ActiveSite != null)
        {
            var idx = App.Sites.FindIndex(s => s.Key == App.ActiveSite.Key);
            if (idx >= 0) SiteSelector.SelectedIndex = idx;
        }
        else if (_siteList.Count > 0)
        {
            SiteSelector.SelectedIndex = 0;
            App.ActiveSite = _siteList[0];
        }
    }

    private async void SiteSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        if (SiteSelector.SelectedItem is SiteSource site && site.Key != App.ActiveSite?.Key)
        {
            _savedScrollOffset = 0;
            App.ActiveSite = site;
            App.Settings.SaveActiveSiteKey(site.Key);
            await LoadHomeDataAsync();
        }
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        _savedScrollOffset = 0;
        await LoadHomeDataAsync();
    }

    private async Task LoadHomeDataAsync()
    {
        if (App.ActiveSite == null) return;
        LoadingBar.Visibility = Visibility.Visible;

        var site = App.ActiveSite;
        var isConfigCenter = site.Key.Contains("baseset") || site.Api.Contains("baseset") || site.Name.Contains("配置");

        if (isConfigCenter)
        {
            // Show In-App Embedded Config Center with full native scrollability
            CategoryTabsRow.Visibility = Visibility.Collapsed;
            VideosScrollViewer.Visibility = Visibility.Collapsed;
            ConfigCenterInAppView.Visibility = Visibility.Visible;
            LoadingBar.Visibility = Visibility.Collapsed;

            await LoadInAppConfigCenterAsync(site);
            return;
        }

        // Standard Video Source Site
        CategoryTabsRow.Visibility = Visibility.Visible;
        ConfigCenterInAppView.Visibility = Visibility.Collapsed;
        VideosScrollViewer.Visibility = Visibility.Visible;

        var result = await App.CatVod.FetchHomeAsync(site);
        LoadingBar.Visibility = Visibility.Collapsed;

        _categories = result.Class;
        _allFilters = result.Filters;

        BuildCategoryTabs();

        _items.Clear();
        foreach (var item in result.List) _items.Add(item);

        _page = result.Page;
        _pageCount = Math.Max(1, result.PageCount);

        PaginationPanel.Visibility = _pageCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        PageText.Text = $"第 {_page} / {_pageCount} 页";
        PrevBtn.IsEnabled = _page > 1;
        NextBtn.IsEnabled = _page < _pageCount;
        EmptyPanel.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task LoadInAppConfigCenterAsync(SiteSource site)
    {
        try
        {
            if (_configCenterWebView == null)
            {
                _configCenterWebView = new Microsoft.UI.Xaml.Controls.WebView2
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                ConfigCenterWebViewContainer.Children.Add(_configCenterWebView);
                await _configCenterWebView.EnsureCoreWebView2Async();
                _configCenterWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            }

            string baseUrl = !string.IsNullOrEmpty(site.ApiBase) ? site.ApiBase.TrimEnd('/') : "http://127.0.0.1:9988";
            string targetUrl = $"{baseUrl}/website";
            _configCenterWebView.CoreWebView2.Navigate(targetUrl);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfigCenterWebView] Load error: {ex.Message}");
        }
    }

    private void ReloadConfigCenter_Click(object sender, RoutedEventArgs e)
    {
        if (_configCenterWebView != null && _configCenterWebView.CoreWebView2 != null)
        {
            _configCenterWebView.CoreWebView2.Reload();
        }
        else if (App.ActiveSite != null)
        {
            _ = LoadInAppConfigCenterAsync(App.ActiveSite);
        }
    }

    private void OpenExternalConfigCenter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string baseUrl = !string.IsNullOrEmpty(App.ActiveSite?.ApiBase) ? App.ActiveSite.ApiBase.TrimEnd('/') : "http://127.0.0.1:9988";
            Process.Start(new ProcessStartInfo($"{baseUrl}/website") { UseShellExecute = true });
        }
        catch { }
    }

    private void BuildCategoryTabs()
    {
        CategoryPanel.Children.Clear();
        FilterGroupsPanel.Children.Clear();
        FilterGroupsContainer.Visibility = Visibility.Collapsed;
        FilterToggleBtn.Visibility = Visibility.Collapsed;
        _currentExtend.Clear();

        if (_categories.Count == 0) return;

        for (int i = 0; i < _categories.Count; i++)
        {
            var cat = _categories[i];
            var pill = new Button
            {
                Content = cat.TypeName,
                Tag = cat.TypeId,
                Style = (i == 0) ? (Style)Application.Current.Resources["AccentButtonStyle"] : (Style)Application.Current.Resources["SubtleButtonStyle"],
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 4, 0),
            };

            pill.Click += async (s, e) =>
            {
                if (s is Button btn && btn.Tag is string tid)
                {
                    _activeCategoryId = tid;
                    _page = 1;
                    _currentExtend.Clear();
                    HighlightActiveCategoryPill(tid);
                    BuildFiltersForCategory(tid);
                    await LoadCategoryAsync();
                }
            };

            CategoryPanel.Children.Add(pill);
        }

        if (_categories.Count > 0)
        {
            _activeCategoryId = _categories[0].TypeId;
            BuildFiltersForCategory(_activeCategoryId);
        }
    }

    private void HighlightActiveCategoryPill(string activeTid)
    {
        foreach (var child in CategoryPanel.Children)
        {
            if (child is Button btn)
            {
                var tid = btn.Tag?.ToString() ?? "";
                btn.Style = (tid == activeTid)
                    ? (Style)Application.Current.Resources["AccentButtonStyle"]
                    : (Style)Application.Current.Resources["SubtleButtonStyle"];
            }
        }
    }

    private void FilterToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        _filterExpanded = !_filterExpanded;
        FilterGroupsPanel.Visibility = _filterExpanded ? Visibility.Visible : Visibility.Collapsed;
        FilterToggleText.Text = _filterExpanded ? "收起筛选 ▴" : "展开筛选 ▾";
    }

    private void BuildFiltersForCategory(string typeId)
    {
        FilterGroupsPanel.Children.Clear();

        if (!_allFilters.TryGetValue(typeId, out var groups) || groups.Count == 0)
        {
            FilterGroupsContainer.Visibility = Visibility.Collapsed;
            FilterToggleBtn.Visibility = Visibility.Collapsed;
            return;
        }

        FilterGroupsContainer.Visibility = Visibility.Visible;
        FilterToggleBtn.Visibility = Visibility.Visible;
        _filterExpanded = true;
        FilterGroupsPanel.Visibility = Visibility.Visible;
        FilterToggleText.Text = "收起筛选 ▴";

        foreach (var group in groups)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 2, 0, 2) };

            var label = new TextBlock
            {
                Text = group.Name,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 55,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                FontSize = 12
            };
            row.Children.Add(label);

            var scroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Enabled,
                VerticalScrollMode = ScrollMode.Disabled
            };

            var itemsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

            foreach (var val in group.Value)
            {
                bool isSelected = string.IsNullOrEmpty(val.V)
                    ? !_currentExtend.ContainsKey(group.Key)
                    : (_currentExtend.TryGetValue(group.Key, out var current) && current == val.V);

                var pill = new Button
                {
                    Content = val.N,
                    Tag = val.V,
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 4, 10, 4),
                    FontSize = 12,
                    Style = isSelected ? (Style)Application.Current.Resources["AccentButtonStyle"] : (Style)Application.Current.Resources["SubtleButtonStyle"]
                };
                pill.Click += async (s, e) =>
                {
                    if (string.IsNullOrEmpty(val.V))
                        _currentExtend.Remove(group.Key);
                    else
                        _currentExtend[group.Key] = val.V;

                    _page = 1;
                    HighlightActiveFilterPills(itemsPanel, val.V);
                    await LoadCategoryAsync();
                };
                itemsPanel.Children.Add(pill);
            }

            scroll.Content = itemsPanel;
            row.Children.Add(scroll);
            FilterGroupsPanel.Children.Add(row);
        }
    }

    private static void HighlightActiveFilterPills(StackPanel panel, string activeValue)
    {
        foreach (var child in panel.Children)
        {
            if (child is Button btn)
            {
                var tag = btn.Tag?.ToString() ?? "";
                btn.Style = (tag == activeValue)
                    ? (Style)Application.Current.Resources["AccentButtonStyle"]
                    : (Style)Application.Current.Resources["SubtleButtonStyle"];
            }
        }
    }

    private async Task LoadCategoryAsync()
    {
        if (App.ActiveSite == null || string.IsNullOrEmpty(_activeCategoryId)) return;
        LoadingBar.Visibility = Visibility.Visible;
        var result = await App.CatVod.FetchCategoryAsync(App.ActiveSite, _activeCategoryId, _page, _currentExtend);
        LoadingBar.Visibility = Visibility.Collapsed;

        if (result.Filters.Count > 0)
        {
            foreach (var kv in result.Filters) _allFilters[kv.Key] = kv.Value;
        }

        _items.Clear();
        foreach (var item in result.List) _items.Add(item);

        _page = result.Page;
        _pageCount = Math.Max(1, result.PageCount);

        PaginationPanel.Visibility = _pageCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        PageText.Text = $"第 {_page} / {_pageCount} 页";
        PrevBtn.IsEnabled = _page > 1;
        NextBtn.IsEnabled = _page < _pageCount;
        EmptyPanel.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ItemsGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is VodItem item)
        {
            if (item.VodId == "config-center" || (App.ActiveSite != null && App.ActiveSite.Api.Contains("baseset")))
            {
                var configSite = App.Sites.FirstOrDefault(s => s.Key == "baseset" || s.Name.Contains("配置"));
                if (configSite != null)
                {
                    App.ActiveSite = configSite;
                    SiteSelector.SelectedItem = configSite;
                }
                return;
            }

            if (App.ActiveSite != null)
                MainWindow.Instance?.NavigateToDetail(item, App.ActiveSite);
        }
    }

    private async void PrevBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_page > 1) { _page--; await LoadCategoryAsync(); }
    }

    private async void NextBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_page < _pageCount) { _page++; await LoadCategoryAsync(); }
    }

    private void ItemsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGridItemSize(ItemsGrid);
    }

    private static void UpdateGridItemSize(GridView gridView)
    {
        if (gridView.ItemsPanelRoot is ItemsWrapGrid wrapGrid && gridView.ActualWidth > 100)
        {
            double totalWidth = gridView.ActualWidth;
            int columns = Math.Max(2, (int)Math.Floor(totalWidth / 170.0));
            double itemWidth = Math.Floor(totalWidth / columns);
            double itemHeight = Math.Floor(itemWidth * 1.5);
            wrapGrid.ItemWidth = itemWidth;
            wrapGrid.ItemHeight = itemHeight;
        }
    }
}
