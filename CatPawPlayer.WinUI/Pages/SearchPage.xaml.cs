using CatPawPlayer.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class SearchPage : Page
{
    private readonly List<string> _searchHistory = [];

    public SearchPage()
    {
        InitializeComponent();
        Loaded += SearchPage_Loaded;
    }

    private void SearchPage_Loaded(object sender, RoutedEventArgs e)
    {
        _searchHistory.Clear();
        _searchHistory.AddRange(App.Settings.LoadSearchHistory());
        RenderHistoryPills();
    }

    private void RenderHistoryPills()
    {
        HistoryPillsPanel.Children.Clear();

        if (_searchHistory.Count == 0)
        {
            HistoryContainer.Visibility = Visibility.Collapsed;
            return;
        }

        HistoryContainer.Visibility = Visibility.Visible;

        foreach (var keyword in _searchHistory.Take(15))
        {
            var kw = keyword;

            var border = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 4, 6, 4),
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBtn = new Button
            {
                Content = kw,
                Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
                Padding = new Thickness(0),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            textBtn.Click += async (s, e) =>
            {
                SearchBox.Text = kw;
                await PerformSearchAsync(kw);
            };
            stack.Children.Add(textBtn);

            var deleteBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE711", FontSize = 10, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorTertiaryBrush"] },
                Style = (Style)Application.Current.Resources["SubtleButtonStyle"],
                Padding = new Thickness(2),
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(deleteBtn, "删除此条历史");
            deleteBtn.Click += (s, e) =>
            {
                App.Settings.RemoveSearchHistory(kw, _searchHistory);
                RenderHistoryPills();
            };
            stack.Children.Add(deleteBtn);

            border.Child = stack;
            HistoryPillsPanel.Children.Add(border);
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var input = sender.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                sender.ItemsSource = _searchHistory.Take(8).ToList();
            }
            else
            {
                var matches = _searchHistory
                    .Where(k => k.Contains(input, StringComparison.OrdinalIgnoreCase))
                    .Take(8)
                    .ToList();
                sender.ItemsSource = matches;
            }
        }
    }

    private async void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string keyword)
        {
            sender.Text = keyword;
            await PerformSearchAsync(keyword);
        }
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var keyword = args.QueryText?.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return;

        await PerformSearchAsync(keyword);
    }

    private async Task PerformSearchAsync(string keyword)
    {
        try
        {
            // Record search history
            App.Settings.AddSearchHistory(keyword, _searchHistory);
            RenderHistoryPills();

            LoadingRing.IsActive = true;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            SearchResultsControl.ItemsSource = null;

            var results = await App.CatVod.FetchAggregateSearchAsync(App.Sites, keyword);

            if (results.Count == 0)
            {
                EmptyStateText.Text = $"未找到关于 \"{keyword}\" 的结果";
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
            else
            {
                SearchResultsControl.ItemsSource = results;
            }
        }
        catch (Exception ex)
        {
            EmptyStateText.Text = $"搜索出错: {ex.Message}";
            EmptyStatePanel.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private void ClearHistoryBtn_Click(object sender, RoutedEventArgs e)
    {
        _searchHistory.Clear();
        App.Settings.SaveSearchHistory(_searchHistory);
        RenderHistoryPills();
    }

    private void LeftScrollBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Parent is Grid grid)
        {
            var scroller = grid.Children.OfType<ScrollViewer>().FirstOrDefault();
            if (scroller != null)
            {
                var targetOffset = Math.Max(0, scroller.HorizontalOffset - 480);
                scroller.ChangeView(targetOffset, null, null, false);
            }
        }
    }

    private void RightScrollBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Parent is Grid grid)
        {
            var scroller = grid.Children.OfType<ScrollViewer>().FirstOrDefault();
            if (scroller != null)
            {
                var targetOffset = scroller.HorizontalOffset + 480;
                scroller.ChangeView(targetOffset, null, null, false);
            }
        }
    }
}
