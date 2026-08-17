namespace CatPawPlayer.WinUI.Models;

public class VodItem
{
    public string VodId { get; set; } = "";
    public string VodName { get; set; } = "";
    public string VodPic { get; set; } = "";
    public string? VodRemarks { get; set; }
    public string? VodActor { get; set; }
    public string? VodDirector { get; set; }
    public string? VodContent { get; set; }
    public string? VodPlayFrom { get; set; }
    public string? VodPlayUrl { get; set; }
    public string? VodYear { get; set; }
    public string? VodArea { get; set; }
    public string? VodDoubanRate { get; set; }
    public string? TypeName { get; set; }
    public string SiteKey { get; set; } = "";
    public SiteSource? Site { get; set; }

    // For UI binding
    public string DisplayName => VodName;
    public string SubText => TypeName ?? VodYear ?? "高清视频";
    public string BadgeText => VodRemarks ?? "";
    public bool HasBadge => !string.IsNullOrEmpty(VodRemarks);
}

public class CategoryItem
{
    public string TypeId { get; set; } = "";
    public string TypeName { get; set; } = "";
}

public class FilterValueItem
{
    public string N { get; set; } = "";
    public string V { get; set; } = "";
}

public class FilterGroup
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public List<FilterValueItem> Value { get; set; } = [];
}

public class CategoryResult
{
    public List<VodItem> List { get; set; } = [];
    public List<CategoryItem> Class { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageCount { get; set; } = 1;
    public int Total { get; set; } = 0;
    public Dictionary<string, List<FilterGroup>> Filters { get; set; } = [];
}

public class PlayResult
{
    public int Parse { get; set; } = 0;
    public string Url { get; set; } = "";
    public Dictionary<string, string>? Header { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SiteSource
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public int Type { get; set; }
    public string Api { get; set; } = "";
    public int Searchable { get; set; } = 1;
    public int QuickSearch { get; set; } = 1;
    public int Filterable { get; set; } = 1;
    public string? Ext { get; set; }
    public string ApiBase { get; set; } = "http://127.0.0.1:9988";

    public string CleanName => Name
        .Replace("[猫源JS]", "")
        .Replace("[CMS]", "")
        .Replace("[猫源]", "")
        .Replace("[JS]", "")
        .Trim();
}

public class SubscriptionConfig
{
    public List<SiteSource> Sites { get; set; } = [];
}

public class SubscriptionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public int SiteCount { get; set; }
    public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public bool IsActive { get; set; }

    public string CleanName => Name
        .Replace("[猫源JS]", "")
        .Replace("[CMS]", "")
        .Replace("[猫源]", "")
        .Replace("[JS]", "")
        .Trim();

    public string SiteCountText => SiteCount > 0 ? $"{SiteCount} 个站点" : "未加载";
}

public class HistoryItem
{
    public string Id { get; set; } = "";
    public string SiteKey { get; set; } = "";
    public string SiteName { get; set; } = "";
    public string VodId { get; set; } = "";
    public string VodName { get; set; } = "";
    public string VodPic { get; set; } = "";
    public string EpName { get; set; } = "";
    public string Url { get; set; } = "";
    public double Progress { get; set; }
    public double Duration { get; set; }
    public long UpdatedAt { get; set; }
    public int ProgressPercent => Duration > 0 ? (int)Math.Min(100, Progress / Duration * 100) : 0;
    public string ProgressText => $"进度 {ProgressPercent}%";
}

public class AggregateSearchResult
{
    public string SiteKey { get; set; } = "";
    public string SiteName { get; set; } = "";
    public List<VodItem> List { get; set; } = [];
}

public class EpisodeItem
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
}

public class PlaySource
{
    public string SourceName { get; set; } = "";
    public List<EpisodeItem> Episodes { get; set; } = [];
    public int EpisodeCount => Episodes.Count;
    public string EpisodeCountText => $"{EpisodeCount} 集";
}
