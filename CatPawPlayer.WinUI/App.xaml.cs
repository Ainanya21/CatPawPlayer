using CatPawPlayer.WinUI.Models;
using CatPawPlayer.WinUI.Services;
using Microsoft.UI.Xaml;
using System.IO;

namespace CatPawPlayer.WinUI;

public partial class App : Application
{
    public static CatVodService CatVod { get; } = new();
    public static SettingsService Settings { get; } = new();
    public static NodeHostService NodeHost { get; } = new();

    // Global app state
    public static List<SiteSource> Sites { get; set; } = [];
    public static SiteSource? ActiveSite { get; set; }
    public static List<SubscriptionItem> Subscriptions { get; set; } = [];
    public static string ActiveSubUrl { get; set; } = "";
    public static List<HistoryItem> History { get; set; } = [];
    public static List<VodItem> Favorites { get; set; } = [];

    private static TaskCompletionSource<bool>? _sitesLoadedTcs;

    private Window? _mainWindow;
    private bool _started;

    public App()
    {
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "[OK] App constructor starting\n");
        InitializeComponent();
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), "[OK] App InitializeComponent completed\n");
        UnhandledException += (s, e) =>
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "startup.log"), $"[UNHANDLED APP EXCEPTION] {e.Exception}\n");
            e.Handled = true;
        };
        StartApp();
    }

    public static async Task EnsureSitesLoadedAsync()
    {
        if (Sites.Count > 0 && ActiveSite != null) return;
        if (_sitesLoadedTcs != null)
        {
            await _sitesLoadedTcs.Task;
            return;
        }

        _sitesLoadedTcs = new TaskCompletionSource<bool>();
        try
        {
            var subUrl = string.IsNullOrEmpty(ActiveSubUrl) ? "https://9280.kstore.vip/cat/index.js.md5" : ActiveSubUrl;

            // Wait for NodeHost server to start responding and load config
            for (int i = 0; i < 20; i++)
            {
                var config = await CatVod.FetchSubscriptionAsync(subUrl);
                if (config != null && config.Sites.Count > 0)
                {
                    Sites = config.Sites;
                    var savedKey = Settings.LoadActiveSiteKey();
                    var savedSite = Sites.FirstOrDefault(s => s.Key == savedKey);
                    ActiveSite = savedSite ?? Sites.FirstOrDefault();
                    _sitesLoadedTcs.TrySetResult(true);
                    return;
                }
                await Task.Delay(350);
            }
            _sitesLoadedTcs.TrySetResult(false);
        }
        catch
        {
            _sitesLoadedTcs.TrySetResult(false);
        }
    }

    private void StartApp()
    {
        if (_started) return;
        _started = true;
        string logPath = Path.Combine(AppContext.BaseDirectory, "startup.log");
        try
        {
            File.AppendAllText(logPath, "[OK] StartApp entered\n");

            // Start Node.js microservice in background
            Task.Run(() => NodeHost.Start());

            // Load persisted data
            Subscriptions = Settings.LoadSubscriptions();
            History = Settings.LoadHistory();
            Favorites = Settings.LoadFavorites();
            ActiveSubUrl = Settings.LoadActiveSubUrl();
            File.AppendAllText(logPath, "[OK] Data loaded\n");

            // Eagerly trigger background site loading
            Task.Run(async () => await EnsureSitesLoadedAsync());

            _mainWindow = new MainWindow();
            File.AppendAllText(logPath, "[OK] MainWindow instantiated\n");
            _mainWindow.Activate();
            File.AppendAllText(logPath, "[OK] MainWindow activated\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[FATAL StartApp] {ex}\n");
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        StartApp();
    }
}
