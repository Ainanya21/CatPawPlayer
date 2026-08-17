using CatPawPlayer.WinUI.Models;
using CatPawPlayer.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System.IO;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class PlayerPage : Page
{
    private VodItem? _vod;
    private SiteSource? _site;
    private string _sourceName = "";
    private EpisodeItem? _episode;
    private string _resolvedPlayUrl = "";
    private Dictionary<string, string>? _resolvedHeaders;
    private StreamMetadata? _streamMetadata;
    private Microsoft.UI.Xaml.Controls.WebView2? _webView;
    private bool _qualityChanging = false;
    private static string _hlsJsCache = "";

    public PlayerPage() => InitializeComponent();

    private static string GetHlsJs()
    {
        if (!string.IsNullOrEmpty(_hlsJsCache)) return _hlsJsCache;
        try
        {
            var p = Path.Combine(AppContext.BaseDirectory, "Assets", "hls.min.js");
            if (File.Exists(p))
            {
                _hlsJsCache = File.ReadAllText(p);
                return _hlsJsCache;
            }
        }
        catch { }
        return "";
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is (VodItem vod, SiteSource site, string sourceName, EpisodeItem episode))
        {
            _vod = vod;
            _site = site;
            _sourceName = sourceName;
            _episode = episode;

            TitleText.Text = vod.VodName;
            EpText.Text = $"{sourceName} · {episode.Name}";

            await InitPlayerAsync();
        }
    }

    private async Task InitPlayerAsync()
    {
        if (_site == null || _episode == null) return;

        LoadingPanel.Visibility = Visibility.Visible;
        ProbingMetaText.Text = "正在获取真实播放地址...";

        // 1. Resolve real play URL
        PlayResult playResult;
        try
        {
            playResult = await App.CatVod.FetchPlayUrlAsync(_site, _sourceName, _episode.Url);
        }
        catch (Exception ex)
        {
            playResult = new PlayResult { Parse = 0, Url = "", ErrorMessage = ex.Message };
        }

        _resolvedPlayUrl = playResult.Url;
        _resolvedHeaders = playResult.Header;

        bool isUnparsedNetdisk = string.IsNullOrEmpty(_resolvedPlayUrl) ||
                                 _resolvedPlayUrl.StartsWith("ey") ||
                                 _resolvedPlayUrl.Contains("pan.quark.cn") ||
                                 _resolvedPlayUrl.Contains("alipan.com") ||
                                 _resolvedPlayUrl.Contains("aliyundrive.com") ||
                                 _resolvedPlayUrl.Contains("115.com/s/") ||
                                 _resolvedPlayUrl.Contains("pan.baidu.com");

        if (isUnparsedNetdisk || !string.IsNullOrEmpty(playResult.ErrorMessage))
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorBar.Severity = InfoBarSeverity.Warning;
            ErrorBar.Title = "需要网盘登录授权";
            ErrorBar.Message = !string.IsNullOrEmpty(playResult.ErrorMessage)
                ? $"⚠️ 提示：{playResult.ErrorMessage}"
                : "该片源为网盘 4K 原画资源，请先在【配置中心】扫码或登录对应网盘账号（夸克/UC/阿里/百度/115），即可实现秒级自动转存与 4K 极速播放！";
            ErrorBar.ActionButton = new Button
            {
                Content = "👉 前往配置中心",
                Style = (Style)Application.Current.Resources["AccentButtonStyle"]
            };
            ((Button)ErrorBar.ActionButton).Click += (_, _) =>
            {
                var configSite = App.Sites.FirstOrDefault(s => s.Key == "baseset" || s.Name.Contains("配置"));
                MainWindow.Instance?.SelectCategory(configSite?.Key ?? "baseset");
            };
            ErrorBar.IsOpen = true;
            return;
        }

        ProbingMetaText.Text = "正在探针解析流分辨率、编码与多清晰度分片...";

        // 2. Asynchronously probe real stream metadata with hintText
        _streamMetadata = await StreamMetadataService.ProbeStreamAsync(_resolvedPlayUrl, _resolvedHeaders, $"{_vod?.VodName} {_episode.Name}");

        // Bind metadata HUD
        BindMetadataHud(_streamMetadata);

        // 3. Create WebView2 and load
        _webView = new Microsoft.UI.Xaml.Controls.WebView2
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        WebViewContainer.Children.Add(_webView);

        try
        {
            await _webView.EnsureCoreWebView2Async();

            _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            string html = BuildPlayerHtml(_resolvedPlayUrl, _vod?.VodName ?? "", _episode.Name, _resolvedHeaders);
            _webView.CoreWebView2.NavigateToString(html);

            LoadingPanel.Visibility = Visibility.Collapsed;
            WebViewContainer.Visibility = Visibility.Visible;

            SaveHistory();
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorBar.Message = $"播放器初始化失败: {ex.Message}";
            ErrorBar.IsOpen = true;
        }
    }

    private void BindMetadataHud(StreamMetadata meta)
    {
        ResBadgeText.Text = meta.PrimaryResolution;
        CodecBadgeText.Text = meta.VideoCodec;
        AudioBadgeText.Text = meta.AudioCodec;

        if (meta.PingLatencyMs > 0)
        {
            LatencyBadgeText.Text = $"{meta.PingLatencyMs}ms";
            LatencyBadge.Visibility = Visibility.Visible;
        }

        if (meta.Tracks.Count > 1)
        {
            _qualityChanging = true;
            QualitySelector.ItemsSource = meta.Tracks;
            QualitySelector.DisplayMemberPath = "SummaryText";
            QualitySelector.SelectedItem = meta.BestTrack;
            QualitySelector.Visibility = Visibility.Visible;
            _qualityChanging = false;
        }
        else
        {
            QualitySelector.Visibility = Visibility.Collapsed;
        }
    }

    private void QualitySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_qualityChanging || _webView?.CoreWebView2 == null) return;

        if (QualitySelector.SelectedItem is StreamQualityTrack track && !string.IsNullOrEmpty(track.Url))
        {
            _resolvedPlayUrl = track.Url;
            string html = BuildPlayerHtml(_resolvedPlayUrl, _vod?.VodName ?? "", _episode?.Name ?? "", _resolvedHeaders);
            _webView.CoreWebView2.NavigateToString(html);
        }
    }

    private void OpenInMpvBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_resolvedPlayUrl)) return;
        var title = $"{_vod?.VodName} - {_episode?.Name} [{_streamMetadata?.PrimaryResolution} {_streamMetadata?.VideoCodec}]";
        var played = MpvPlayerService.PlayWithMpv(_resolvedPlayUrl, title, _resolvedHeaders);
        if (played)
        {
            _webView?.CoreWebView2?.ExecuteScriptAsync("document.getElementById('v')?.pause();");
        }
    }

    private static string HtmlEncode(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string BuildPlayerHtml(string url, string title, string epName, Dictionary<string, string>? headers = null)
    {
        string hlsJs = GetHlsJs();
        bool isHls = url.Contains(".m3u8") || url.Contains("m3u8") || !url.Contains(".mp4");

        string hlsScript = !string.IsNullOrEmpty(hlsJs)
            ? $"<script>{hlsJs}</script>"
            : @"<script src=""https://cdnjs.cloudflare.com/ajax/libs/hls.js/1.5.8/hls.min.js""></script>";

        string customHeadersJs = "{}";
        if (headers != null && headers.Count > 0)
        {
            customHeadersJs = Newtonsoft.Json.JsonConvert.SerializeObject(headers);
        }

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>{HtmlEncode(title)} - {HtmlEncode(epName)}</title>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; background:#000; }}
  body, html {{ width:100vw; height:100vh; overflow:hidden; background:#000; display:flex; align-items:center; justify-content:center; }}
  video {{ width:100%; height:100%; outline:none; object-fit:contain; }}
  #error-box {{ display:none; position:absolute; color:#ff6b6b; font-family:system-ui,sans-serif; text-align:center; padding:20px; z-index:100; }}
  #retry-btn {{ margin-top:12px; padding:8px 16px; background:#0078d4; color:#fff; border:none; border-radius:6px; cursor:pointer; font-size:14px; }}
</style>
</head>
<body>
<video id=""v"" controls autoplay playsinline crossorigin=""anonymous""></video>
<div id=""error-box"">
  <div id=""error-msg"">流媒体加载失败，请重试或切换播放源</div>
  <button id=""retry-btn"" onclick=""location.reload()"">🔄 重新加载</button>
</div>
{hlsScript}
<script>
  var video = document.getElementById('v');
  var errorBox = document.getElementById('error-box');
  var errorMsg = document.getElementById('error-msg');
  var playUrl = '{url}';
  var customHeaders = {customHeadersJs};

  function showError(msg) {{
    errorMsg.innerText = msg;
    errorBox.style.display = 'block';
  }}

  if (window.Hls && Hls.isSupported() && (playUrl.indexOf('.m3u8') !== -1 || playUrl.indexOf('m3u8') !== -1 || {isHls.ToString().ToLower()})) {{
    var hls = new Hls({{
      enableWorker: true,
      lowLatencyMode: true,
      xhrSetup: function(xhr, u) {{
        for (var k in customHeaders) {{
          try {{ xhr.setRequestHeader(k, customHeaders[k]); }} catch(e) {{}}
        }}
      }}
    }});
    hls.loadSource(playUrl);
    hls.attachMedia(video);
    hls.on(Hls.Events.MANIFEST_PARSED, function() {{
      video.play().catch(function(e) {{ console.log('Autoplay deferred:', e); }});
    }});
    hls.on(Hls.Events.ERROR, function(event, data) {{
      if (data.fatal) {{
        switch (data.type) {{
          case Hls.ErrorTypes.NETWORK_ERROR:
            hls.startLoad();
            break;
          case Hls.ErrorTypes.MEDIA_ERROR:
            hls.recoverMediaError();
            break;
          default:
            hls.destroy();
            showError('流媒体解码失败，建议使用右上角 MPV 硬件加速播放');
            break;
        }}
      }}
    }});
  }} else {{
    video.src = playUrl;
    video.play().catch(function(e) {{ console.log('Native autoplay deferred:', e); }});
    video.onerror = function() {{
      showError('视频解析失败，请尝试使用右上角 MPV 播放器打开');
    }};
  }}
</script>
</body>
</html>";
    }

    private void SaveHistory()
    {
        if (_vod == null || _site == null || _episode == null) return;
        var histId = $"{_site.Key}_{_vod.VodId}";
        var histItem = new HistoryItem
        {
            Id = histId,
            SiteKey = _site.Key,
            SiteName = _site.CleanName,
            VodId = _vod.VodId,
            VodName = _vod.VodName,
            VodPic = _vod.VodPic,
            EpName = _episode.Name,
            Url = _resolvedPlayUrl,
            Progress = 0,
            Duration = 0,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        App.Settings.UpsertHistory(histItem, App.History);
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        _webView?.CoreWebView2?.Navigate("about:blank");
        Frame.GoBack();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _webView?.CoreWebView2?.Navigate("about:blank");
    }
}
