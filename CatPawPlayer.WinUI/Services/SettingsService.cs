using System.Text.Json;
using CatPawPlayer.WinUI.Models;
using Windows.Storage;

namespace CatPawPlayer.WinUI.Services;

/// <summary>
/// Persists app settings, subscriptions, history, favorites, and search history to local storage.
/// </summary>
public class SettingsService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CatPawPlayer");

    private readonly string _subsFile;
    private readonly string _historyFile;
    private readonly string _favoritesFile;
    private readonly string _settingsFile;
    private readonly string _searchHistoryFile;

    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    public SettingsService()
    {
        Directory.CreateDirectory(DataDir);
        _subsFile = Path.Combine(DataDir, "subscriptions.json");
        _historyFile = Path.Combine(DataDir, "history.json");
        _favoritesFile = Path.Combine(DataDir, "favorites.json");
        _settingsFile = Path.Combine(DataDir, "settings.json");
        _searchHistoryFile = Path.Combine(DataDir, "search_history.json");
    }

    // ── Subscriptions ──────────────────────────────────────────────────
    public List<SubscriptionItem> LoadSubscriptions()
    {
        if (!File.Exists(_subsFile)) return GetDefaultSubscriptions();
        try
        {
            var json = File.ReadAllText(_subsFile);
            return JsonSerializer.Deserialize<List<SubscriptionItem>>(json) ?? GetDefaultSubscriptions();
        }
        catch { return GetDefaultSubscriptions(); }
    }

    public void SaveSubscriptions(List<SubscriptionItem> subs)
    {
        File.WriteAllText(_subsFile, JsonSerializer.Serialize(subs, _jsonOpts));
    }

    private static List<SubscriptionItem> GetDefaultSubscriptions() =>
    [
        new() { Id = "default_0", Name = "王二小牛娃猫源 index.js.md5", Url = "https://9280.kstore.vip/cat/index.js.md5" },
        new() { Id = "default_1", Name = "多仓订阅源 (FongMi)", Url = "https://raw.githubusercontent.com/FongMi/CatVodSpider/main/json/config.json" },
        new() { Id = "default_2", Name = "饭太硬 TVBox", Url = "http://饭太硬.top/tv" },
    ];

    // ── History ────────────────────────────────────────────────────────
    public List<HistoryItem> LoadHistory()
    {
        if (!File.Exists(_historyFile)) return [];
        try
        {
            var json = File.ReadAllText(_historyFile);
            return JsonSerializer.Deserialize<List<HistoryItem>>(json) ?? [];
        }
        catch { return []; }
    }

    public void SaveHistory(List<HistoryItem> history)
    {
        File.WriteAllText(_historyFile, JsonSerializer.Serialize(history, _jsonOpts));
    }

    public void UpsertHistory(HistoryItem item, List<HistoryItem> history)
    {
        var idx = history.FindIndex(h => h.Id == item.Id);
        if (idx >= 0) history[idx] = item;
        else history.Insert(0, item);
        // Keep max 200
        if (history.Count > 200) history.RemoveRange(200, history.Count - 200);
        SaveHistory(history);
    }

    // ── Favorites ──────────────────────────────────────────────────────
    public List<VodItem> LoadFavorites()
    {
        if (!File.Exists(_favoritesFile)) return [];
        try
        {
            var json = File.ReadAllText(_favoritesFile);
            return JsonSerializer.Deserialize<List<VodItem>>(json) ?? [];
        }
        catch { return []; }
    }

    public void SaveFavorites(List<VodItem> favorites)
    {
        File.WriteAllText(_favoritesFile, JsonSerializer.Serialize(favorites, _jsonOpts));
    }

    // ── Search History ──────────────────────────────────────────────────
    public List<string> LoadSearchHistory()
    {
        if (!File.Exists(_searchHistoryFile)) return [];
        try
        {
            var json = File.ReadAllText(_searchHistoryFile);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch { return []; }
    }

    public void SaveSearchHistory(List<string> history)
    {
        File.WriteAllText(_searchHistoryFile, JsonSerializer.Serialize(history, _jsonOpts));
    }

    public void AddSearchHistory(string keyword, List<string> history)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        keyword = keyword.Trim();
        history.RemoveAll(k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        history.Insert(0, keyword);
        if (history.Count > 30) history.RemoveRange(30, history.Count - 30);
        SaveSearchHistory(history);
    }

    public void RemoveSearchHistory(string keyword, List<string> history)
    {
        history.RemoveAll(k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        SaveSearchHistory(history);
    }

    // ── App Settings ────────────────────────────────────────────────────
    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFile)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(_settingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void SaveSettings(AppSettings settings)
    {
        File.WriteAllText(_settingsFile, JsonSerializer.Serialize(settings, _jsonOpts));
    }

    // ── Active Site Key ─────────────────────────────────────────────────
    public string LoadActiveSiteKey()
    {
        var s = LoadSettings();
        return s.ActiveSiteKey ?? "";
    }

    public void SaveActiveSiteKey(string key)
    {
        var s = LoadSettings();
        s.ActiveSiteKey = key;
        SaveSettings(s);
    }

    public string LoadActiveSubUrl()
    {
        var s = LoadSettings();
        return s.ActiveSubUrl ?? "";
    }

    public void SaveActiveSubUrl(string url)
    {
        var s = LoadSettings();
        s.ActiveSubUrl = url;
        SaveSettings(s);
    }
}

public class AppSettings
{
    public string? ActiveSiteKey { get; set; }
    public string? ActiveSubUrl { get; set; }
    public string ThemeMode { get; set; } = "Default"; // Default, Light, Dark
    public string? Theme { get => ThemeMode; set => ThemeMode = value ?? "Default"; }
    public string AccentColor { get; set; } = "#6366f1";
    public string? QuarkCookie { get; set; }
    public string? BaiduCookie { get; set; }
    public string? Cookie115 { get; set; }
    public string MpvPath { get; set; } = "";
    public bool UseExternalMpv { get; set; } = false;
}
