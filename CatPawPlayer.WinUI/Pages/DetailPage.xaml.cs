using CatPawPlayer.WinUI.Models;
using CatPawPlayer.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System.IO;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class DetailPage : Page
{
    private VodItem? _vod;
    private SiteSource? _site;
    private bool _isFavorite = false;

    public DetailPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is (VodItem vod, SiteSource site))
        {
            _vod = vod;
            _site = site;
            _isFavorite = App.Favorites.Any(f => f.VodId == _vod.VodId);
            UpdateFavoriteBtn();

            var settings = App.Settings.LoadSettings();
            MpvSwitch.IsOn = settings.UseExternalMpv && MpvPlayerService.IsMpvAvailable;

            await LoadDetailAsync();
        }
    }

    private async Task LoadDetailAsync()
    {
        if (_vod == null || _site == null) return;

        LoadingRing.IsActive = true;
        ContentScroller.Visibility = Visibility.Collapsed;

        var detail = await App.CatVod.FetchDetailAsync(_site, _vod.VodId);
        if (detail != null) _vod = detail;

        LoadingRing.IsActive = false;
        ContentScroller.Visibility = Visibility.Visible;

        RenderDetail();
    }

    private void RenderDetail()
    {
        if (_vod == null) return;

        TitleText.Text = _vod.VodName;

        if (!string.IsNullOrEmpty(_vod.VodPic))
        {
            try
            {
                var hdPic = _vod.VodPic;
                if (hdPic.Contains("doubanio.com"))
                {
                    hdPic = hdPic.Replace("/s_ratio_poster/", "/l_ratio_poster/")
                                 .Replace("/m_ratio_poster/", "/l_ratio_poster/")
                                 .Replace("/photo/s/", "/photo/l/")
                                 .Replace("/photo/m/", "/photo/l/");
                }
                CoverImage.Source = new BitmapImage(new Uri(hdPic));
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(_vod.VodYear))
        {
            YearText.Text = _vod.VodYear;
            YearBadge.Visibility = Visibility.Visible;
        }

        if (!string.IsNullOrEmpty(_vod.VodArea))
        {
            AreaText.Text = _vod.VodArea;
            AreaBadge.Visibility = Visibility.Visible;
        }

        if (!string.IsNullOrEmpty(_vod.VodDoubanRate))
        {
            RateText.Text = $"★ {_vod.VodDoubanRate}";
            RateBadge.Visibility = Visibility.Visible;
        }

        MetaPanel.Children.Clear();
        if (!string.IsNullOrEmpty(_vod.VodDirector))
            MetaPanel.Children.Add(CreateMetaRow("导演", _vod.VodDirector));
        if (!string.IsNullOrEmpty(_vod.VodActor))
            MetaPanel.Children.Add(CreateMetaRow("主演", _vod.VodActor));
        if (!string.IsNullOrEmpty(_vod.VodRemarks))
            MetaPanel.Children.Add(CreateMetaRow("更新", _vod.VodRemarks));

        ContentText.Text = string.IsNullOrEmpty(_vod.VodContent) ? "暂无剧情简介" : _vod.VodContent.Trim();

        BuildEpisodePivot();
    }

    private static TextBlock CreateMetaRow(string label, string value)
    {
        return new TextBlock
        {
            Text = $"{label}: {value}",
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        };
    }

    private void BuildEpisodePivot()
    {
        if (_vod == null) return;
        SourcePivot.Items.Clear();

        var fromList = _vod.VodPlayFrom?.Split("$$$") ?? [];
        var urlList = _vod.VodPlayUrl?.Split("$$$") ?? [];

        for (int i = 0; i < fromList.Length; i++)
        {
            var sourceName = fromList[i];
            var urlStr = i < urlList.Length ? urlList[i] : "";
            var episodes = ParseEpisodes(urlStr);
            if (episodes.Count == 0) continue;

            var wrapPanel = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, MaximumRowsOrColumns = 100 };
            var scrollViewer = new ScrollViewer { Content = wrapPanel, MaxHeight = 260 };

            foreach (var ep in episodes)
            {
                var epCopy = ep;
                var btn = new Button
                {
                    Content = ep.Name,
                    Margin = new Thickness(4),
                    MinWidth = 70,
                    Tag = epCopy,
                };
                btn.Click += async (_, _) =>
                {
                    if (_vod == null || _site == null) return;

                    StatusInfoBar.Severity = InfoBarSeverity.Informational;
                    StatusInfoBar.Title = "解析中";
                    StatusInfoBar.Message = $"正在探测解析资源：{_vod.VodName} - {epCopy.Name}...";
                    StatusInfoBar.ActionButton = null;
                    StatusInfoBar.IsOpen = true;

                    var playRes = await App.CatVod.FetchPlayUrlAsync(_site, sourceName, epCopy.Url);
                    var realUrl = playRes.Url;

                    bool isUnparsedNetdisk = string.IsNullOrEmpty(realUrl) ||
                                             realUrl.StartsWith("ey") ||
                                             realUrl.Contains("pan.quark.cn") ||
                                             realUrl.Contains("alipan.com") ||
                                             realUrl.Contains("aliyundrive.com") ||
                                             realUrl.Contains("115.com/s/") ||
                                             realUrl.Contains("pan.baidu.com");

                    if (isUnparsedNetdisk || !string.IsNullOrEmpty(playRes.ErrorMessage))
                    {
                        string promptMsg = !string.IsNullOrEmpty(playRes.ErrorMessage)
                            ? $"⚠️ 提示：{playRes.ErrorMessage}"
                            : "该片源为网盘原画 4K 资源，请先在【配置中心】扫码或登录对应网盘账号（夸克/UC/阿里/百度/115），即可实现秒级自动转存与 4K 极速播放！";

                        StatusInfoBar.Severity = InfoBarSeverity.Warning;
                        StatusInfoBar.Title = "需网盘账号授权";
                        StatusInfoBar.Message = promptMsg;

                        var goConfigBtn = new Button
                        {
                            Content = "👉 立即前往配置中心扫码",
                            Style = (Style)Application.Current.Resources["AccentButtonStyle"]
                        };
                        goConfigBtn.Click += (s, ev) =>
                        {
                            var configSite = App.Sites.FirstOrDefault(s => s.Key == "baseset" || s.Name.Contains("配置"));
                            MainWindow.Instance?.SelectCategory(configSite?.Key ?? "baseset");
                        };
                        StatusInfoBar.ActionButton = goConfigBtn;
                        StatusInfoBar.IsOpen = true;
                        return;
                    }

                    // Asynchronously probe real stream metadata with full hint context
                    var streamMeta = await StreamMetadataService.ProbeStreamAsync(realUrl, playRes.Header, $"{_vod.VodName} {epCopy.Name}");

                    // Update UI StreamMetaCard
                    MetaResText.Text = streamMeta.PrimaryResolution;
                    MetaCodecText.Text = streamMeta.VideoCodec;
                    MetaAudioText.Text = streamMeta.AudioCodec;
                    if (streamMeta.PingLatencyMs > 0)
                    {
                        MetaLatencyText.Text = $"{streamMeta.PingLatencyMs}ms";
                        MetaLatencyBadge.Visibility = Visibility.Visible;
                    }
                    StreamMetaCard.Visibility = Visibility.Visible;

                    if (MpvSwitch.IsOn && MpvPlayerService.IsMpvAvailable)
                    {
                        var title = $"{_vod.VodName} - {epCopy.Name} [{streamMeta.PrimaryResolution} {streamMeta.VideoCodec}]";
                        var played = MpvPlayerService.PlayWithMpv(realUrl, title, playRes.Header);
                        if (played)
                        {
                            StatusInfoBar.Severity = InfoBarSeverity.Success;
                            StatusInfoBar.Title = "MPV 播放就绪";
                            StatusInfoBar.Message = $"🚀 已在 Yaozhi-MPV 硬件加速播放器中打开 ({streamMeta.PrimaryResolution} {streamMeta.VideoCodec})";

                            // Upsert history
                            App.Settings.UpsertHistory(new HistoryItem
                            {
                                Id = $"{_vod.VodId}_{epCopy.Name}",
                                SiteKey = _site.Key,
                                SiteName = _site.CleanName,
                                VodId = _vod.VodId,
                                VodName = _vod.VodName,
                                VodPic = _vod.VodPic,
                                EpName = epCopy.Name,
                                Url = realUrl,
                                Progress = 0,
                                Duration = 100,
                                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            }, App.History);
                        }
                        else
                        {
                            StatusInfoBar.Severity = InfoBarSeverity.Error;
                            StatusInfoBar.Title = "MPV 启动失败";
                            StatusInfoBar.Message = "启动外部 MPV 播放器失败，正在切换至内置原生播放器...";
                            Frame.Navigate(typeof(PlayerPage), (_vod, _site, sourceName, epCopy));
                        }
                    }
                    else
                    {
                        Frame.Navigate(typeof(PlayerPage), (_vod, _site, sourceName, epCopy));
                    }
                };
                wrapPanel.Children.Add(btn);
            }

            var pivotItem = new PivotItem
            {
                Header = $"{sourceName} ({episodes.Count})",
                Content = scrollViewer,
            };
            SourcePivot.Items.Add(pivotItem);
        }
    }

    private static List<EpisodeItem> ParseEpisodes(string urlStr)
    {
        var list = new List<EpisodeItem>();
        if (string.IsNullOrWhiteSpace(urlStr)) return list;

        var entries = urlStr.Split('#', StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            var parts = entry.Split('$', 2);
            if (parts.Length == 2)
            {
                list.Add(new EpisodeItem { Name = parts[0].Trim(), Url = parts[1].Trim() });
            }
            else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                list.Add(new EpisodeItem { Name = $"第 {list.Count + 1} 集", Url = parts[0].Trim() });
            }
        }
        return list;
    }

    private void FavoriteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_vod == null) return;
        _isFavorite = !_isFavorite;

        if (_isFavorite)
        {
            if (!App.Favorites.Any(f => f.VodId == _vod.VodId))
                App.Favorites.Insert(0, _vod);
        }
        else
        {
            App.Favorites.RemoveAll(f => f.VodId == _vod.VodId);
        }

        App.Settings.SaveFavorites(App.Favorites);
        UpdateFavoriteBtn();
    }

    private void UpdateFavoriteBtn()
    {
        FavoriteIcon.Glyph = _isFavorite ? "\uE735" : "\uE734";
        FavoriteText.Text = _isFavorite ? "已收藏" : "收藏";
        FavoriteBtn.Style = _isFavorite ? (Style)Application.Current.Resources["AccentButtonStyle"] : (Style)Application.Current.Resources["SubtleButtonStyle"];
    }

    private void MpvSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        var settings = App.Settings.LoadSettings();
        settings.UseExternalMpv = MpvSwitch.IsOn;
        App.Settings.SaveSettings(settings);
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
