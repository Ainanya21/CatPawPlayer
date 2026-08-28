using CatPawPlayer.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class HomePage : Page
{
    private readonly List<VodItem> _heroItems = [];
    private readonly ObservableCollection<VodItem> _showcaseItems = [];
    private readonly ObservableCollection<SiteSource> _siteList = [];
    private int _currentHeroIndex = 0;
    private readonly DispatcherTimer _heroTimer = new();
    private bool _isLoading = false;
    private bool _initialized = false;
    private double _savedScrollOffset = 0;

    public HomePage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        _heroTimer.Interval = TimeSpan.FromSeconds(5);
        _heroTimer.Tick += (s, e) => NextBanner();
        Loaded += HomePage_Loaded;
        Unloaded += (s, e) => _heroTimer.Stop();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back && _initialized)
        {
            RestoreScrollPosition();
        }
    }

    protected override void OnNavigatingFrom(Microsoft.UI.Xaml.Navigation.NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        SaveScrollPosition();
    }

    public void SaveScrollPosition()
    {
        if (ContentScroller != null)
        {
            _savedScrollOffset = ContentScroller.VerticalOffset;
        }
    }

    public void RestoreScrollPosition()
    {
        if (_savedScrollOffset > 0 && ContentScroller != null)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(60);
                ContentScroller.ChangeView(null, _savedScrollOffset, null, true);
            });
        }
    }

    private async void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            if (_heroItems.Count > 0 && !_heroTimer.IsEnabled) _heroTimer.Start();
            RestoreScrollPosition();
            return;
        }

        LoadingRing.IsActive = true;

        if (App.Sites.Count == 0 || App.ActiveSite == null)
        {
            await App.EnsureSitesLoadedAsync();
        }

        PopulateSiteSelector();
        await LoadHomeAsync();
        _initialized = true;
        UpdateGridItemSize(ShowcaseGrid);
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

    private void SiteSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;

        if (SiteSelector.SelectedItem is SiteSource site && site.Key != App.ActiveSite?.Key)
        {
            App.ActiveSite = site;
            App.Settings.SaveActiveSiteKey(site.Key);

            // Navigate to CategoryPage on source selection
            MainWindow.Instance?.SelectCategory();
        }
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        await LoadHomeAsync();
    }

    private async Task LoadHomeAsync()
    {
        if (App.ActiveSite == null || _isLoading)
        {
            LoadingRing.IsActive = false;
            return;
        }

        _isLoading = true;
        LoadingRing.IsActive = true;
        ContentScroller.Visibility = Visibility.Collapsed;
        EmptyPanel.Visibility = Visibility.Collapsed;
        _heroTimer.Stop();

        try
        {
            var site = App.ActiveSite;
            var result = await App.CatVod.FetchHomeAsync(site);

            if (result.List.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                ContentScroller.Visibility = Visibility.Visible;
                return;
            }

            // Hero Banner (first 5 items)
            _heroItems.Clear();
            _heroItems.AddRange(result.List.Take(5));
            _currentHeroIndex = 0;

            if (_heroItems.Count > 0)
            {
                DisplayHeroItem(_currentHeroIndex, true);
                BannerContainer.Visibility = Visibility.Visible;
                _heroTimer.Start();
            }
            else
            {
                BannerContainer.Visibility = Visibility.Collapsed;
            }

            // Showcase grid
            _showcaseItems.Clear();
            foreach (var item in result.List) _showcaseItems.Add(item);
            ShowcaseGrid.ItemsSource = _showcaseItems;
            ShowcaseTitle.Text = $"⚡ {site.CleanName} — 最新到库 ({result.List.Count})";

            ShowcasePanel.Visibility = Visibility.Visible;
            ContentScroller.Visibility = Visibility.Visible;
        }
        catch { }
        finally
        {
            LoadingRing.IsActive = false;
            _isLoading = false;
            UpdateGridItemSize(ShowcaseGrid);
        }
    }

    private void ShowcaseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGridItemSize(ShowcaseGrid);
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

    private static string GetHighResPicUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (url.Contains("doubanio.com"))
        {
            return url.Replace("/s_ratio_poster/", "/l_ratio_poster/")
                      .Replace("/m_ratio_poster/", "/l_ratio_poster/")
                      .Replace("/photo/s/", "/photo/l/")
                      .Replace("/photo/m/", "/photo/l/");
        }
        return url;
    }

    private void DisplayHeroItem(int index, bool isNext)
    {
        if (_heroItems.Count == 0 || index < 0 || index >= _heroItems.Count) return;
        var item = _heroItems[index];

        HeroTitle.Text = item.VodName;
        HeroSubText.Text = string.IsNullOrEmpty(item.VodRemarks) ? item.SubText : $"{item.VodRemarks} · {item.SubText}";

        var hdPic = GetHighResPicUrl(item.VodPic);
        if (!string.IsNullOrEmpty(hdPic))
        {
            try
            {
                var bmp = new BitmapImage(new Uri(hdPic));
                HeroBgImage.Source = bmp;
                HeroPosterImage.Source = bmp;
            }
            catch { }
        }

        if (item.HasBadge)
        {
            HeroBadgeText.Text = item.BadgeText;
            HeroBadge.Visibility = Visibility.Visible;
        }
        else
        {
            HeroBadge.Visibility = Visibility.Collapsed;
        }

        AnimateBannerTransition(isNext);
    }

    private void AnimateBannerTransition(bool isNext)
    {
        var storyboard = new Storyboard();

        // Smooth Opacity Fade In
        var fade = new DoubleAnimation
        {
            From = 0.2,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(320),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fade, HeroContentArea);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        // Smooth Horizontal Slide In
        var slide = new DoubleAnimation
        {
            From = isNext ? 40.0 : -40.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(320),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slide, HeroTranslate);
        Storyboard.SetTargetProperty(slide, "X");
        storyboard.Children.Add(slide);

        storyboard.Begin();
    }

    private void PrevBanner_Click(object sender, RoutedEventArgs e)
    {
        if (_heroItems.Count == 0) return;
        _currentHeroIndex = (_currentHeroIndex - 1 + _heroItems.Count) % _heroItems.Count;
        DisplayHeroItem(_currentHeroIndex, false);
        _heroTimer.Stop();
        _heroTimer.Start();
    }

    private void NextBanner_Click(object sender, RoutedEventArgs e)
    {
        NextBanner();
    }

    private void NextBanner()
    {
        if (_heroItems.Count == 0) return;
        _currentHeroIndex = (_currentHeroIndex + 1) % _heroItems.Count;
        DisplayHeroItem(_currentHeroIndex, true);
        _heroTimer.Stop();
        _heroTimer.Start();
    }

    private void HeroPlayBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_heroItems.Count > 0 && _currentHeroIndex < _heroItems.Count && App.ActiveSite != null)
        {
            var item = _heroItems[_currentHeroIndex];
            MainWindow.Instance?.NavigateToDetail(item, App.ActiveSite);
        }
    }

    private void HeroDetailBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_heroItems.Count > 0 && _currentHeroIndex < _heroItems.Count && App.ActiveSite != null)
        {
            SaveScrollPosition();
            var item = _heroItems[_currentHeroIndex];
            MainWindow.Instance?.NavigateToDetail(item, App.ActiveSite);
        }
    }

    private void ShowcaseGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is VodItem item && App.ActiveSite != null)
        {
            SaveScrollPosition();
            MainWindow.Instance?.NavigateToDetail(item, App.ActiveSite);
        }
    }

    private void GoToSubscription_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.SelectSubscription();
    }
}
