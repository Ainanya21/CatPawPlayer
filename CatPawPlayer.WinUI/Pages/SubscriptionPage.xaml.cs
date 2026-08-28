using CatPawPlayer.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class SubscriptionPage : Page
{
    private readonly ObservableCollection<SubscriptionItem> _subs = [];
    private readonly ObservableCollection<SiteSource> _sites = [];

    public SubscriptionPage()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        _subs.Clear();
        foreach (var s in App.Subscriptions)
        {
            s.IsActive = s.Url == App.ActiveSubUrl;
            _subs.Add(s);
        }
        SubListView.ItemsSource = _subs;
        SubCountText.Text = $"共 {_subs.Count} 条";

        _sites.Clear();
        foreach (var s in App.Sites) _sites.Add(s);
        SitesListView.ItemsSource = _sites;
        SitesHeader.Text = $"当前激活源包含站点列表 ({_sites.Count} 个)";
    }

    private async void AddSubBtn_Click(object sender, RoutedEventArgs e)
    {
        var url = SubUrlBox.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        AddSubBtn.IsEnabled = false;
        StatusBar.IsOpen = false;

        try
        {
            var config = await App.CatVod.FetchSubscriptionAsync(url);
            if (config == null || config.Sites.Count == 0)
            {
                ShowStatus("警告", "订阅解析失败：格式不符合 TVBox/CatPaw 规范或站点列表为空。", InfoBarSeverity.Warning);
                return;
            }

            var existing = App.Subscriptions.FirstOrDefault(s => s.Url == url);
            if (existing != null)
            {
                existing.SiteCount = config.Sites.Count;
            }
            else
            {
                var newSub = new SubscriptionItem
                {
                    Name = $"订阅源 ({url[^Math.Min(20, url.Length)..]})",
                    Url = url,
                    SiteCount = config.Sites.Count,
                };
                App.Subscriptions.Insert(0, newSub);
            }

            App.Sites = config.Sites;
            App.ActiveSubUrl = url;
            App.ActiveSite = config.Sites.FirstOrDefault();
            App.Settings.SaveSubscriptions(App.Subscriptions);
            App.Settings.SaveActiveSubUrl(url);

            SubUrlBox.Text = "";
            LoadData();
            ShowStatus("成功", $"已切换至订阅源，共加载 {config.Sites.Count} 个站点。", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus("错误", $"导入失败: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            AddSubBtn.IsEnabled = true;
        }
    }

    private async void SubListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SubscriptionItem sub) return;
        await LoadSubscriptionAsync(sub.Url, sub.Name);
    }

    private async void RefreshSub_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            var sub = App.Subscriptions.FirstOrDefault(s => s.Id == id);
            if (sub != null) await LoadSubscriptionAsync(sub.Url, sub.Name);
        }
    }

    private async void RefreshAllBtn_Click(object sender, RoutedEventArgs e)
    {
        RefreshAllBtn.IsEnabled = false;
        ShowStatus("更新中", "正在从云端拉取所有订阅源与爬虫更新...", InfoBarSeverity.Informational);

        int successCount = 0;
        foreach (var sub in App.Subscriptions.ToList())
        {
            try
            {
                var cfg = await App.CatVod.FetchSubscriptionAsync(sub.Url);
                if (cfg != null && cfg.Sites.Count > 0)
                {
                    sub.SiteCount = cfg.Sites.Count;
                    if (sub.Url == App.ActiveSubUrl)
                    {
                        App.Sites = cfg.Sites;
                        App.ActiveSite = cfg.Sites.FirstOrDefault();
                    }
                    successCount++;
                }
            }
            catch { }
        }

        App.Settings.SaveSubscriptions(App.Subscriptions);
        LoadData();
        RefreshAllBtn.IsEnabled = true;
        ShowStatus("同步完成", $"已成功检查并刷新全部 {successCount} 个订阅源！", InfoBarSeverity.Success);
    }

    private async Task LoadSubscriptionAsync(string url, string name)
    {
        StatusBar.IsOpen = false;
        try
        {
            var config = await App.CatVod.FetchSubscriptionAsync(url);
            if (config == null || config.Sites.Count == 0)
            {
                ShowStatus("警告", "无法加载该订阅源", InfoBarSeverity.Warning);
                return;
            }

            var sub = App.Subscriptions.FirstOrDefault(s => s.Url == url);
            if (sub != null) sub.SiteCount = config.Sites.Count;

            App.Sites = config.Sites;
            App.ActiveSubUrl = url;
            App.ActiveSite = config.Sites.FirstOrDefault();
            App.Settings.SaveSubscriptions(App.Subscriptions);
            App.Settings.SaveActiveSubUrl(url);

            LoadData();
            ShowStatus("成功", $"已切换至 [{name}]，共 {config.Sites.Count} 个站点。", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus("错误", ex.Message, InfoBarSeverity.Error);
        }
    }

    private void DeleteSub_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            var sub = App.Subscriptions.FirstOrDefault(s => s.Id == id);
            if (sub != null)
            {
                App.Subscriptions.Remove(sub);
                App.Settings.SaveSubscriptions(App.Subscriptions);
                LoadData();
            }
        }
    }

    private void SitesListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SiteSource site)
        {
            if (site.Key.Contains("baseset") || site.Api.Contains("baseset") || site.Name.Contains("配置"))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://127.0.0.1:9988/website") { UseShellExecute = true });
                }
                catch { }
                return;
            }

            App.ActiveSite = site;
            App.Settings.SaveActiveSiteKey(site.Key);
            MainWindow.Instance?.SelectCategory();
        }
    }

    private void ShowStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusBar.Title = title;
        StatusBar.Message = message;
        StatusBar.Severity = severity;
        StatusBar.IsOpen = true;
    }
}

