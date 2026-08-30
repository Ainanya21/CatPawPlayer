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
            var episodes = ParseEpisodes(urlStr, _vod.VodName);
            if (episodes.Count == 0) continue;

            var wrapPanel = new VariableSizedWrapGrid { Orientation = Orientation.Horizontal, MaximumRowsOrColumns = 100 };
            var scrollViewer = new ScrollViewer { Content = wrapPanel, MaxHeight = 260 };

            for (int epIdx = 0; epIdx < episodes.Count; epIdx++)
            {
                var epCopy = episodes[epIdx];
                int currentIdx = epIdx;
                var btn = new Button
                {
                    Content = epCopy.Name,
                    Margin = new Thickness(4),
                    MinWidth = 100,
                    Padding = new Thickness(10, 6, 10, 6),
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
                        var played = MpvPlayerService.PlayWithMpvPlaylist(
                            episodes,
                            currentIdx,
                            _vod.VodName,
                            _site,
                            sourceName,
                            realUrl,
                            playRes.Header);

                        if (played)
                        {
                            StatusInfoBar.Severity = InfoBarSeverity.Success;
                            StatusInfoBar.Title = "MPV 播放就绪";
                            StatusInfoBar.Message = $"🚀 已在 Yaozhi-MPV 硬件加速播放器中打开 ({streamMeta.PrimaryResolution} {streamMeta.VideoCodec})，全量剧集已同步至播放列表";

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

    private static List<EpisodeItem> ParseEpisodes(string urlStr, string vodName = "")
    {
        var list = new List<EpisodeItem>();
        if (string.IsNullOrWhiteSpace(urlStr)) return list;

        var entries = urlStr.Split('#', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var parts = entry.Split('$', 2);
            string rawName = "";
            string rawUrl = "";

            if (parts.Length == 2)
            {
                rawName = parts[0].Trim();
                rawUrl = parts[1].Trim();
            }
            else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                rawName = "";
                rawUrl = parts[0].Trim();
            }

            string cleanName = CleanEpisodeName(rawName, vodName, i + 1, entries.Length);
            list.Add(new EpisodeItem { Name = cleanName, Url = rawUrl });
        }
        return list;
    }

    private static string CleanEpisodeName(string rawName, string vodName, int epIndex, int totalEpisodes)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return totalEpisodes == 1 ? "正片" : $"第 {epIndex} 集";
        }

        // 1. Extract File Size (e.g. "1.85 GB", "2.1G", "850 MB", "950MB")
        string sizeTag = "";
        var sizeMatch = System.Text.RegularExpressions.Regex.Match(
            rawName,
            @"(?i)(?:\[|\(|\s|^|-|_)(\d+(?:\.\d+)?\s*(?:GB|MB|G|M|GiB|MiB))(?:\)|\]|\s|-|_|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (sizeMatch.Success)
        {
            sizeTag = sizeMatch.Groups[1].Value.Trim().ToUpper();
        }

        // 2. Build cleaned working copy for episode extraction by removing interfering noise
        string cleanWork = rawName;

        // CRITICAL: Strip file sizes FIRST while decimal points (e.g. 1.1GB, 2.7GB) are intact
        cleanWork = System.Text.RegularExpressions.Regex.Replace(cleanWork, @"(?i)(?:\[|\(|\s|^|-|_)?\d+(?:\.\d+)?\s*(?:GB|MB|G|M|GiB|MiB)(?:\]|\)|\s|-|_|$)?", " ");

        // Strip common resolutions, codecs, fps and media specs
        cleanWork = System.Text.RegularExpressions.Regex.Replace(cleanWork, @"(?i)(?:4K|2160P|1080P|720P|540P|480P|60FPS|120FPS|10BIT|8BIT|HDR\d*|DOLBY\s*VISION|DV|H\.?265|H\.?264|HEVC|AVC|AV1|AAC|DDP\d*(?:\.\d+)?|REMUX|WEB-DL|BLURAY|HDTC|HD|SDR|高码率)", " ");

        // Strip video extensions
        cleanWork = System.Text.RegularExpressions.Regex.Replace(cleanWork, @"(?i)\.(?:mp4|mkv|flv|ts|avi|mov|wmv|m3u8|iso)$", "");

        // Strip release years (e.g. 1990 - 2030)
        cleanWork = System.Text.RegularExpressions.Regex.Replace(cleanWork, @"\b(19\d\d|20[0-3]\d)\b", " ");

        // Convert brackets, punctuation and symbols to spaces
        cleanWork = System.Text.RegularExpressions.Regex.Replace(cleanWork, @"[\[\]\(\)\{\}【】_\-\.]", " ");

        // Strip vod name if present to avoid digits in title
        if (!string.IsNullOrWhiteSpace(vodName))
        {
            cleanWork = cleanWork.Replace(vodName, " ", StringComparison.OrdinalIgnoreCase);
        }

        // 3. Check for specials (预告/花絮/彩蛋/番外/特辑/SP/OVA/上集/下集/大结局)
        var specialMatch = System.Text.RegularExpressions.Regex.Match(
            rawName,
            @"(预告片?|花絮|彩蛋|番外篇?|特辑|SP\d*|OVA\d*|上集|下集|大结局|导视|宣传片|和谐不补)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (specialMatch.Success)
        {
            string special = specialMatch.Value;
            return string.IsNullOrEmpty(sizeTag) ? special : $"{special} ({sizeTag})";
        }

        // 4. Extract Episode Number
        int epNum = -1;

        // Pattern 1: Explicit episode keyword (第05集, EP05, E05, 第05话, 05集, 05话)
        var epPattern1 = System.Text.RegularExpressions.Regex.Match(
            cleanWork,
            @"(?i)(?:(?:第|EP|E)\s*(\d{1,4})|(\d{1,4})\s*(?:集|话))",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (epPattern1.Success)
        {
            string val = !string.IsNullOrEmpty(epPattern1.Groups[1].Value) ? epPattern1.Groups[1].Value : epPattern1.Groups[2].Value;
            if (int.TryParse(val, out int n1))
            {
                epNum = n1;
            }
        }

        // Pattern 2: Any 1-4 digit number
        if (epNum <= 0)
        {
            var epPattern2 = System.Text.RegularExpressions.Regex.Match(
                cleanWork,
                @"\b0*(\d{1,4})\b");
            if (epPattern2.Success && int.TryParse(epPattern2.Groups[1].Value, out int n2))
            {
                epNum = n2;
            }
        }

        // 5. Sanity check: Fallback to epIndex if no episode number found
        if (epNum <= 0)
        {
            epNum = epIndex;
        }

        // 6. Single episode movie handling
        if (totalEpisodes == 1 && epNum == 1)
        {
            if (rawName.Contains("正片") || rawName.Contains("4K") || rawName.Contains("原画") || rawName.Length <= 6)
            {
                return string.IsNullOrEmpty(sizeTag) ? "正片" : $"正片 ({sizeTag})";
            }
        }

        return string.IsNullOrEmpty(sizeTag) ? $"第 {epNum} 集" : $"第 {epNum} 集 ({sizeTag})";
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
