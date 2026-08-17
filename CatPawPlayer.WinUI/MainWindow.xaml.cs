using CatPawPlayer.WinUI.Pages;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CatPawPlayer.WinUI;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        SetupMicaBackdrop();
        SetupTitleBar();

        // Apply user-configured theme on startup
        var settings = App.Settings.LoadSettings();
        SetAppTheme(settings.Theme ?? "Default");

        NavView.SelectedItem = NavHome;
        ContentFrame.Navigate(typeof(HomePage));
    }

    public void SetAppTheme(string theme)
    {
        var elementTheme = theme switch
        {
            "Dark" => ElementTheme.Dark,
            "Light" => ElementTheme.Light,
            _ => ElementTheme.Default
        };

        RootGrid.RequestedTheme = elementTheme;
    }

    private void SetupTitleBar()
    {
        Title = "猫爪播放器";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
    }

    private void SetupMicaBackdrop()
    {
        this.SystemBackdrop = new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base };
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            Type? pageType = tag switch
            {
                "subscription" => typeof(SubscriptionPage),
                "home" => typeof(HomePage),
                "category" => typeof(CategoryPage),
                "search" => typeof(SearchPage),
                "history" => typeof(HistoryPage),
                "favorites" => typeof(FavoritesPage),
                "settings" => typeof(SettingsPage),
                _ => null
            };

            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }

    public void SelectCategory(string? siteKey = null)
    {
        NavView.SelectedItem = NavCategory;
        ContentFrame.Navigate(typeof(CategoryPage), siteKey);
    }

    public void SelectSubscription()
    {
        NavView.SelectedItem = NavSubscription;
        ContentFrame.Navigate(typeof(SubscriptionPage));
    }

    public void NavigateToDetail(Models.VodItem item, Models.SiteSource site)
    {
        ContentFrame.Navigate(typeof(DetailPage), (item, site));
    }

    public void NavigateToPlayer(Models.VodItem item, Models.SiteSource site, string sourceName, Models.EpisodeItem episode)
    {
        ContentFrame.Navigate(typeof(PlayerPage), (item, site, sourceName, episode));
    }
}
