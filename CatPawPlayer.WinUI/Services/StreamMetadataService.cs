using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using CatPawPlayer.WinUI.Models;

namespace CatPawPlayer.WinUI.Services;

public static class StreamMetadataService
{
    private static readonly HttpClient _http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    })
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task<StreamMetadata> ProbeStreamAsync(string mediaUrl, Dictionary<string, string>? headers = null, string hintText = "")
    {
        var meta = new StreamMetadata
        {
            RawUrl = mediaUrl,
            FinalUrl = mediaUrl,
        };

        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            InferDirectMediaMetadata(meta, "", hintText);
            return meta;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
            req.Headers.Add("User-Agent", "okhttp/4.9.0");
            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    if (kv.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)) continue;
                    req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }

            var sw = Stopwatch.StartNew();
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            sw.Stop();

            meta.PingLatencyMs = sw.ElapsedMilliseconds;
            meta.FinalUrl = resp.RequestMessage?.RequestUri?.ToString() ?? mediaUrl;

            if (resp.Content.Headers.ContentLength.HasValue)
            {
                meta.ContentSizeBytes = resp.Content.Headers.ContentLength.Value;
            }

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";

            // Check if HLS or direct stream
            bool isM3u8 = mediaUrl.Contains(".m3u8", StringComparison.OrdinalIgnoreCase) ||
                          contentType.Contains("mpegurl", StringComparison.OrdinalIgnoreCase) ||
                          contentType.Contains("x-mpegurl", StringComparison.OrdinalIgnoreCase);

            if (isM3u8)
            {
                meta.StreamFormat = "HLS 流 (.m3u8)";
                var streamContent = await resp.Content.ReadAsStringAsync();
                ParseHlsManifest(meta, streamContent, meta.FinalUrl, hintText);
            }
            else if (mediaUrl.Contains(".mp4", StringComparison.OrdinalIgnoreCase) || contentType.Contains("mp4"))
            {
                meta.StreamFormat = "MP4 视频";
                InferDirectMediaMetadata(meta, mediaUrl, hintText);
            }
            else if (mediaUrl.Contains(".flv", StringComparison.OrdinalIgnoreCase) || contentType.Contains("flv"))
            {
                meta.StreamFormat = "FLV 直播流";
                InferDirectMediaMetadata(meta, mediaUrl, hintText);
            }
            else
            {
                meta.StreamFormat = "直链流媒体";
                InferDirectMediaMetadata(meta, mediaUrl, hintText);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StreamMetadataService.ProbeStreamAsync] Exception: {ex.Message}");
            InferDirectMediaMetadata(meta, mediaUrl, hintText);
        }

        return meta;
    }

    private static void ParseHlsManifest(StreamMetadata meta, string manifest, string baseUrl, string hintText = "")
    {
        if (string.IsNullOrEmpty(manifest)) return;

        if (manifest.Contains("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
        {
            meta.IsMasterPlaylist = true;
            var lines = manifest.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                {
                    var track = new StreamQualityTrack();
                    var attrs = line.Substring("#EXT-X-STREAM-INF:".Length);

                    var resMatch = Regex.Match(attrs, @"RESOLUTION=(\d+)x(\d+)", RegexOptions.IgnoreCase);
                    if (resMatch.Success)
                    {
                        track.Width = int.Parse(resMatch.Groups[1].Value);
                        track.Height = int.Parse(resMatch.Groups[2].Value);
                    }

                    var bwMatch = Regex.Match(attrs, @"BANDWIDTH=(\d+)", RegexOptions.IgnoreCase);
                    if (bwMatch.Success)
                    {
                        track.Bandwidth = long.Parse(bwMatch.Groups[1].Value);
                    }

                    var fpsMatch = Regex.Match(attrs, @"FRAME-RATE=([\d\.]+)", RegexOptions.IgnoreCase);
                    if (fpsMatch.Success)
                    {
                        track.FrameRate = double.Parse(fpsMatch.Groups[1].Value);
                    }

                    var codecMatch = Regex.Match(attrs, @"CODECS=""([^""]+)""", RegexOptions.IgnoreCase);
                    if (codecMatch.Success)
                    {
                        track.Codecs = codecMatch.Groups[1].Value;
                        DecodeCodecsString(track.Codecs, out var vc, out var ac, out var hdr);
                        track.VideoCodec = vc;
                        track.AudioCodec = ac;
                        if (hdr) meta.HasHdr = true;
                    }

                    if (i + 1 < lines.Count && !lines[i + 1].StartsWith("#"))
                    {
                        track.Url = ResolveAbsoluteUrl(baseUrl, lines[i + 1]);
                    }

                    track.Name = track.QualityBadge;
                    meta.Tracks.Add(track);
                }
            }

            if (meta.Tracks.Count > 0)
            {
                var best = meta.BestTrack;
                if (best != null)
                {
                    meta.PrimaryResolution = best.QualityBadge;
                    if (!string.IsNullOrEmpty(best.VideoCodec)) meta.VideoCodec = best.VideoCodec;
                    if (!string.IsNullOrEmpty(best.AudioCodec)) meta.AudioCodec = best.AudioCodec;
                    if (best.FrameRate > 0) meta.FrameRateText = $"{Math.Round(best.FrameRate)} FPS";
                    if (best.Bandwidth > 0) meta.BitrateText = best.BitrateLabel;
                }
            }
        }
        else
        {
            meta.IsMasterPlaylist = false;
            InferDirectMediaMetadata(meta, baseUrl, hintText);
        }
    }

    private static void DecodeCodecsString(string codecs, out string videoCodec, out string audioCodec, out bool hasHdr)
    {
        videoCodec = "H.264 / AVC";
        audioCodec = "AAC";
        hasHdr = false;

        var lower = codecs.ToLowerInvariant();

        if (lower.Contains("hvc1") || lower.Contains("hev1") || lower.Contains("h265") || lower.Contains("hevc"))
            videoCodec = "HEVC / H.265";
        else if (lower.Contains("av01") || lower.Contains("av1"))
            videoCodec = "AV1 超清";
        else if (lower.Contains("vp09") || lower.Contains("vp9"))
            videoCodec = "VP9";
        else if (lower.Contains("avc1") || lower.Contains("h264"))
            videoCodec = "H.264 / AVC";

        if (lower.Contains("ec-3") || lower.Contains("eac3"))
            audioCodec = "Dolby Digital Plus (E-AC-3)";
        else if (lower.Contains("ac-3") || lower.Contains("ac3"))
            audioCodec = "Dolby Digital (AC-3)";
        else if (lower.Contains("mp4a"))
            audioCodec = "AAC 高清音频";
        else if (lower.Contains("flac"))
            audioCodec = "FLAC 无损音频";
        else if (lower.Contains("opus"))
            audioCodec = "Opus";

        if (lower.Contains("hvc1.2") || lower.Contains("hev1.2") || lower.Contains("hdr") || lower.Contains("10bit"))
            hasHdr = true;
    }

    private static void InferDirectMediaMetadata(StreamMetadata meta, string url, string hintText = "")
    {
        var lower = $"{url} {hintText}".ToLowerInvariant();

        // Detect resolution
        if (lower.Contains("2160p") || lower.Contains("4k") || lower.Contains("3840x2160") || lower.Contains("3840*2160") || lower.Contains("uhd"))
            meta.PrimaryResolution = "4K UHD";
        else if (lower.Contains("1440p") || lower.Contains("2k") || lower.Contains("2560x1440") || lower.Contains("2560*1440") || lower.Contains("qhd"))
            meta.PrimaryResolution = "2K QHD";
        else if (lower.Contains("1080p") || lower.Contains("1920x1080") || lower.Contains("1920*1080") || lower.Contains("fhd") || lower.Contains("原画"))
            meta.PrimaryResolution = "1080P 原画";
        else if (lower.Contains("720p") || lower.Contains("1280x720") || lower.Contains("hd") || lower.Contains("高清"))
            meta.PrimaryResolution = "720P HD";
        else if (lower.Contains("480p"))
            meta.PrimaryResolution = "480P";
        else
            meta.PrimaryResolution = "1080P 极速";

        // Detect Video Codec
        if (lower.Contains("hevc") || lower.Contains("h265") || lower.Contains("x265") || lower.Contains("h.265") || lower.Contains("265") || lower.Contains("4k"))
            meta.VideoCodec = "HEVC / H.265";
        else if (lower.Contains("av1") || lower.Contains("av01"))
            meta.VideoCodec = "AV1 超清";
        else if (lower.Contains("h264") || lower.Contains("x264") || lower.Contains("h.264") || lower.Contains("avc"))
            meta.VideoCodec = "H.264 / AVC";
        else
            meta.VideoCodec = "H.264 / AVC";

        // Detect HDR / Dolby
        if (lower.Contains("hdr") || lower.Contains("hdr10") || lower.Contains("dovi") || lower.Contains("10bit") || lower.Contains("杜比视界"))
            meta.HasHdr = true;

        if (lower.Contains("atmos") || lower.Contains("dolby") || lower.Contains("dts") || lower.Contains("5.1") || lower.Contains("7.1") || lower.Contains("杜比全景声"))
        {
            meta.HasDolby = true;
            meta.AudioCodec = "杜比多声道音频";
        }
        else
        {
            meta.AudioCodec = "AAC 高清音频";
        }

        // Frame rate
        if (lower.Contains("60fps") || lower.Contains("60帧") || lower.Contains("60p"))
            meta.FrameRateText = "60 FPS";
        else if (lower.Contains("120fps") || lower.Contains("120帧"))
            meta.FrameRateText = "120 FPS";
        else
            meta.FrameRateText = "动态帧率";
    }

    private static string ResolveAbsoluteUrl(string baseUrl, string relativeUrl)
    {
        if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _)) return relativeUrl;
        if (Uri.TryCreate(new Uri(baseUrl), relativeUrl, out var combined))
            return combined.ToString();
        return relativeUrl;
    }
}
