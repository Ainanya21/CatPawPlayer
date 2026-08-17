namespace CatPawPlayer.WinUI.Models;

public class StreamQualityTrack
{
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bandwidth { get; set; }
    public double FrameRate { get; set; }
    public string Codecs { get; set; } = "";
    public string VideoCodec { get; set; } = "";
    public string AudioCodec { get; set; } = "";
    public string Url { get; set; } = "";

    public string ResolutionLabel => Width > 0 && Height > 0 ? $"{Width}×{Height}" : "";

    public string QualityBadge => Height switch
    {
        >= 2160 => "4K UHD",
        >= 1440 => "2K QHD",
        >= 1080 => "1080P",
        >= 720 => "720P",
        >= 480 => "480P",
        _ => "标清"
    };

    public string BitrateLabel => Bandwidth > 0 ? $"{Bandwidth / 1000000.0:F1} Mbps" : "";
    public string SummaryText => $"{QualityBadge} · {VideoCodec} · {BitrateLabel}".Trim(' ', '·');
}

public class StreamMetadata
{
    public string RawUrl { get; set; } = "";
    public string FinalUrl { get; set; } = "";
    public string StreamFormat { get; set; } = "HLS (.m3u8)";
    public bool IsMasterPlaylist { get; set; }
    public List<StreamQualityTrack> Tracks { get; set; } = [];

    public StreamQualityTrack? BestTrack => Tracks.OrderByDescending(t => t.Height).ThenByDescending(t => t.Bandwidth).FirstOrDefault();

    public string PrimaryResolution { get; set; } = "1080P";
    public string VideoCodec { get; set; } = "HEVC / H.265";
    public string AudioCodec { get; set; } = "AAC";
    public string FrameRateText { get; set; } = "60 FPS";
    public string BitrateText { get; set; } = "";
    public long PingLatencyMs { get; set; } = 0;
    public long ContentSizeBytes { get; set; } = 0;

    public string ContentSizeText => ContentSizeBytes > 0 ? $"{ContentSizeBytes / (1024.0 * 1024 * 1024):F2} GB" : "";
    public string LatencyBadge => PingLatencyMs > 0 ? $"{PingLatencyMs}ms" : "";
    public bool HasHdr { get; set; }
    public bool HasDolby { get; set; }

    public string CodecBadge => string.IsNullOrEmpty(VideoCodec) ? "H.264" : VideoCodec;
}
