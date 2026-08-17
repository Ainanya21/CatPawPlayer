using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CatPawPlayer.WinUI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CatPawPlayer.WinUI.Services;

public class CatVodService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private const string DefaultBaseUrl = "http://127.0.0.1:9988";
    private const string GatewayUrl = "http://127.0.0.1:9980";

    private static string GetSpiderEndpoint(SiteSource site, string action)
    {
        string baseUrl = !string.IsNullOrEmpty(site.ApiBase) ? site.ApiBase.TrimEnd('/') : DefaultBaseUrl;
        if (action == "player") action = "play";
        if (site.Api.StartsWith("/spider/"))
            return $"{baseUrl}{site.Api.TrimEnd('/')}/{action}";
        if (site.Api.StartsWith("csp_"))
            return $"{baseUrl}/spider/{site.Api.Replace("csp_", "").ToLower()}/3/{action}";
        return $"{baseUrl}/spider/{site.Key.Replace("nodejs_", "").ToLower()}/3/{action}";
    }

    // ─── Subscription ────────────────────────────────────────────────────
    public async Task<SubscriptionConfig?> FetchSubscriptionAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        try
        {
            string resp = "";

            // 1. Try sending subscription load command to local Control Gateway (Port 9980)
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { url }),
                    Encoding.UTF8,
                    "application/json");
                var postResp = await _http.PostAsync($"{GatewayUrl}/subscription/load", content);
                if (postResp.IsSuccessStatusCode)
                {
                    resp = await postResp.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CatVodService.GatewayLoad] {ex.Message}");
            }

            // 2. Fallback to direct HTTP fetch
            if (string.IsNullOrEmpty(resp) || resp.StartsWith("{\"error\""))
            {
                try
                {
                    resp = await _http.GetStringAsync(url);
                }
                catch { }
            }

            var obj = TryParseJson(resp);
            if (obj == null) return null;

            var config = new SubscriptionConfig();
            var sitesArr = (obj["sites"] as JArray) ?? (obj["video"]?["sites"] as JArray);
            if (sitesArr != null)
            {
                foreach (var s in sitesArr)
                {
                    string defaultApiBase = (url.Contains("douer") || url.Contains("catpaw.douer.me"))
                        ? "http://127.0.0.1:2333"
                        : "http://127.0.0.1:9988";

                    config.Sites.Add(new SiteSource
                    {
                        Key = s["key"]?.ToString() ?? Guid.NewGuid().ToString(),
                        Name = s["name"]?.ToString() ?? "未知站点",
                        Type = s["type"]?.ToObject<int>() ?? 1,
                        Api = s["api"]?.ToString() ?? "",
                        Searchable = s["searchable"]?.ToObject<int>() ?? 1,
                        QuickSearch = s["quickSearch"]?.ToObject<int>() ?? 1,
                        Filterable = s["filterable"]?.ToObject<int>() ?? 1,
                        Ext = s["ext"]?.ToString(),
                        ApiBase = s["apiBase"]?.ToString() ?? defaultApiBase,
                    });
                }
            }
            return config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CatVodService.FetchSubscription] {ex.Message}");
            return null;
        }
    }

    // ─── Home ─────────────────────────────────────────────────────────────
    public async Task<CategoryResult> FetchHomeAsync(SiteSource site)
    {
        try
        {
            if (site.Type == 3)
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { }),
                    Encoding.UTF8, "application/json");
                var endpoint = GetSpiderEndpoint(site, "home");
                var r = await _http.PostAsync(endpoint, content);
                var json = await r.Content.ReadAsStringAsync();
                return ParseCategoryResult(json);
            }

            var res = await _http.GetStringAsync($"{site.Api}?ac=detail");
            return ParseCategoryResult(res);
        }
        catch { return new CategoryResult(); }
    }

    // ─── Category ─────────────────────────────────────────────────────────
    public async Task<CategoryResult> FetchCategoryAsync(SiteSource site, string tid, int page, Dictionary<string, string>? extend = null)
    {
        try
        {
            if (site.Type == 3)
            {
                var body = new { tid, pg = page, extend };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var endpoint = GetSpiderEndpoint(site, "category");
                var r = await _http.PostAsync(endpoint, content);
                return ParseCategoryResult(await r.Content.ReadAsStringAsync());
            }

            var url = $"{site.Api}?ac=detail&t={tid}&pg={page}";
            if (extend != null)
                foreach (var kv in extend)
                    url += $"&{kv.Key}={Uri.EscapeDataString(kv.Value)}";

            var res = await _http.GetStringAsync(url);
            return ParseCategoryResult(res);
        }
        catch { return new CategoryResult(); }
    }

    // ─── Detail ───────────────────────────────────────────────────────────
    public async Task<VodItem?> FetchDetailAsync(SiteSource site, string vodId)
    {
        try
        {
            if (site.Type == 3)
            {
                var body = new { id = vodId, ids = new[] { vodId } };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var endpoint = GetSpiderEndpoint(site, "detail");
                var r = await _http.PostAsync(endpoint, content);
                var result = ParseCategoryResult(await r.Content.ReadAsStringAsync());
                if (result.List.Count == 0) return null;

                // 1. If an item directly has play sources, prioritize it
                var playable = result.List.FirstOrDefault(i => !string.IsNullOrEmpty(i.VodPlayFrom) && !string.IsNullOrEmpty(i.VodPlayUrl));
                if (playable != null)
                {
                    DecodeBase64Metadata(playable, vodId);
                    return playable;
                }

                // 2. Multi-Folder Parallel Probe
                var best = result.List[0];
                var subFolderTasks = result.List.Select(async subItem =>
                {
                    if (subItem.VodId != vodId && !string.IsNullOrEmpty(subItem.VodId))
                    {
                        try
                        {
                            var subBody = new { id = subItem.VodId, ids = new[] { subItem.VodId } };
                            var subContent = new StringContent(JsonConvert.SerializeObject(subBody), Encoding.UTF8, "application/json");
                            var subResp = await _http.PostAsync(endpoint, subContent);
                            var subRes = ParseCategoryResult(await subResp.Content.ReadAsStringAsync());
                            return subRes.List.FirstOrDefault(x => !string.IsNullOrEmpty(x.VodPlayFrom) && !string.IsNullOrEmpty(x.VodPlayUrl));
                        }
                        catch { return null; }
                    }
                    return null;
                });

                var subItems = await Task.WhenAll(subFolderTasks);
                var allPlayFrom = new List<string>();
                var allPlayUrl = new List<string>();

                foreach (var item in subItems)
                {
                    if (item != null && !string.IsNullOrEmpty(item.VodPlayFrom) && !string.IsNullOrEmpty(item.VodPlayUrl))
                    {
                        allPlayFrom.Add(item.VodPlayFrom);
                        allPlayUrl.Add(item.VodPlayUrl);
                        if (string.IsNullOrEmpty(best.VodActor) && !string.IsNullOrEmpty(item.VodActor)) best.VodActor = item.VodActor;
                        if (string.IsNullOrEmpty(best.VodDirector) && !string.IsNullOrEmpty(item.VodDirector)) best.VodDirector = item.VodDirector;
                        if (string.IsNullOrEmpty(best.VodContent) && !string.IsNullOrEmpty(item.VodContent)) best.VodContent = item.VodContent;
                        if (string.IsNullOrEmpty(best.VodYear) && !string.IsNullOrEmpty(item.VodYear)) best.VodYear = item.VodYear;
                        if (string.IsNullOrEmpty(best.VodArea) && !string.IsNullOrEmpty(item.VodArea)) best.VodArea = item.VodArea;
                    }
                }

                if (allPlayFrom.Count > 0)
                {
                    best.VodPlayFrom = string.Join("$$$", allPlayFrom);
                    best.VodPlayUrl = string.Join("$$$", allPlayUrl);
                }

                DecodeBase64Metadata(best, vodId);
                return best;
            }

            var res = await _http.GetStringAsync($"{site.Api}?ac=detail&ids={vodId}");
            var cr = ParseCategoryResult(res);
            return cr.List.FirstOrDefault();
        }
        catch { return null; }
    }

    private static void DecodeBase64Metadata(VodItem item, string vodId)
    {
        try
        {
            var raw = item.VodId;
            if (string.IsNullOrEmpty(raw) || !raw.Contains("ey")) raw = vodId;
            if (raw.StartsWith("gy:")) raw = raw.Substring(3);

            if (raw.StartsWith("ey"))
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(raw));
                var obj = TryParseJson(json);
                if (obj != null)
                {
                    var name = obj["name"]?.ToString();
                    if (!string.IsNullOrEmpty(name) && (string.IsNullOrEmpty(item.VodName) || item.VodName.Contains("网盘") || item.VodName.Contains("线路")))
                        item.VodName = name;

                    if (string.IsNullOrEmpty(item.VodYear)) item.VodYear = obj["year"]?.ToString();
                    if (string.IsNullOrEmpty(item.VodArea)) item.VodArea = obj["area"]?.ToString();
                    if (string.IsNullOrEmpty(item.VodActor)) item.VodActor = obj["actor"]?.ToString();
                    if (string.IsNullOrEmpty(item.VodDirector)) item.VodDirector = obj["director"]?.ToString();
                    if (string.IsNullOrEmpty(item.VodContent)) item.VodContent = obj["content"]?.ToString();
                    if (string.IsNullOrEmpty(item.TypeName)) item.TypeName = obj["type"]?.ToString();
                }
            }
        }
        catch { }
    }

    // ─── Search ───────────────────────────────────────────────────────────
    public async Task<List<VodItem>> FetchSearchAsync(SiteSource site, string keyword)
    {
        try
        {
            if (site.Type == 3)
            {
                var body = new { key = keyword, quick = 0 };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var endpoint = GetSpiderEndpoint(site, "search");
                var r = await _http.PostAsync(endpoint, content);
                var result = ParseCategoryResult(await r.Content.ReadAsStringAsync());
                return result.List;
            }

            var res = await _http.GetStringAsync($"{site.Api}?ac=detail&wd={Uri.EscapeDataString(keyword)}");
            return ParseCategoryResult(res).List;
        }
        catch { return []; }
    }

    // ─── Aggregate Search ─────────────────────────────────────────────────
    public async Task<List<AggregateSearchResult>> FetchAggregateSearchAsync(IEnumerable<SiteSource> sites, string keyword)
    {
        var searchableSites = sites.Where(s => s.Searchable == 1).Take(15).ToList();
        var tasks = searchableSites.Select(async site =>
        {
            var list = await FetchSearchAsync(site, keyword);
            foreach (var item in list)
            {
                item.Site = site;
                item.SiteKey = site.Key;
            }
            return new AggregateSearchResult { SiteKey = site.Key, SiteName = site.CleanName, List = list };
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.List.Count > 0).ToList();
    }

    // ─── Play URL ─────────────────────────────────────────────────────────
    public async Task<PlayResult> FetchPlayUrlAsync(SiteSource site, string flag, string playId)
    {
        try
        {
            if (site.Type == 3)
            {
                var body = new { flag, id = playId };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var endpoint = GetSpiderEndpoint(site, "play");
                var r = await _http.PostAsync(endpoint, content);
                var json = await r.Content.ReadAsStringAsync();
                var obj = TryParseJson(json);

                // If empty with original flag, try normalized flag (e.g. "夸克4K" -> "quark")
                if (obj == null || (string.IsNullOrEmpty(obj["url"]?.ToString()) && obj["message"] == null))
                {
                    string simplifiedFlag = NormalizeFlag(flag);
                    if (simplifiedFlag != flag)
                    {
                        var retryBody = new { flag = simplifiedFlag, id = playId };
                        var retryContent = new StringContent(JsonConvert.SerializeObject(retryBody), Encoding.UTF8, "application/json");
                        var retryR = await _http.PostAsync(endpoint, retryContent);
                        var retryJson = await retryR.Content.ReadAsStringAsync();
                        var retryObj = TryParseJson(retryJson);
                        if (retryObj != null)
                        {
                            obj = retryObj;
                        }
                    }
                }

                // If spider returned an explicit error message (e.g. "还没有配置夸克 Cookie")
                if (obj != null && obj["message"] != null && obj["url"] == null)
                {
                    return new PlayResult
                    {
                        Parse = 0,
                        Url = "",
                        ErrorMessage = obj["message"]?.ToString() ?? "网盘未授权或解析失败"
                    };
                }

                if (obj != null && !string.IsNullOrEmpty(obj["url"]?.ToString()))
                {
                    var rawUrl = obj["url"]?.ToString() ?? "";
                    var headers = new Dictionary<string, string>();

                    if (obj["header"] is JObject headerObj)
                    {
                        foreach (var prop in headerObj.Properties())
                        {
                            headers[prop.Name] = prop.Value.ToString();
                        }
                    }

                    return new PlayResult
                    {
                        Parse = obj["parse"]?.ToObject<int>() ?? 0,
                        Url = rawUrl,
                        Header = headers.Count > 0 ? headers : null
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new PlayResult { Parse = 0, Url = "", ErrorMessage = ex.Message };
        }
        return new PlayResult { Parse = 0, Url = playId.StartsWith("ey") ? "" : playId };
    }

    private static string NormalizeFlag(string flag)
    {
        var lower = flag.ToLower();
        if (lower.Contains("quark") || lower.Contains("夸克")) return "quark";
        if (lower.Contains("ali") || lower.Contains("阿里")) return "ali";
        if (lower.Contains("115")) return "115";
        if (lower.Contains("uc")) return "uc";
        if (lower.Contains("baidu") || lower.Contains("百度")) return "baidu";
        if (lower.Contains("wogg") || lower.Contains("玩偶")) return "wogg";
        if (lower.Contains("muou") || lower.Contains("木偶")) return "muou";
        return flag;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────
    private static CategoryResult ParseCategoryResult(string json)
    {
        try
        {
            var obj = TryParseJson(json);
            if (obj == null) return new CategoryResult();

            var result = new CategoryResult
            {
                Page = obj["page"]?.ToObject<int>() ?? 1,
                PageCount = obj["pagecount"]?.ToObject<int>() ?? 1,
                Total = obj["total"]?.ToObject<int>() ?? 0,
            };

            var listArr = obj["list"] as JArray;
            if (listArr != null)
            {
                foreach (var item in listArr)
                {
                    result.List.Add(new VodItem
                    {
                        VodId = item["vod_id"]?.ToString() ?? "",
                        VodName = item["vod_name"]?.ToString() ?? "",
                        VodPic = item["vod_pic"]?.ToString() ?? "",
                        VodRemarks = item["vod_remarks"]?.ToString(),
                        VodActor = item["vod_actor"]?.ToString(),
                        VodDirector = item["vod_director"]?.ToString(),
                        VodContent = item["vod_content"]?.ToString(),
                        VodPlayFrom = item["vod_play_from"]?.ToString(),
                        VodPlayUrl = item["vod_play_url"]?.ToString(),
                        VodYear = item["vod_year"]?.ToString(),
                        VodArea = item["vod_area"]?.ToString(),
                        VodDoubanRate = item["vod_douban_score"]?.ToString(),
                        TypeName = item["type_name"]?.ToString(),
                    });
                }
            }

            var classArr = obj["class"] as JArray;
            if (classArr != null)
            {
                foreach (var c in classArr)
                {
                    result.Class.Add(new CategoryItem
                    {
                        TypeId = c["type_id"]?.ToString() ?? "",
                        TypeName = c["type_name"]?.ToString() ?? "",
                    });
                }
            }

            var filtersObj = obj["filters"] as JObject;
            if (filtersObj != null)
            {
                foreach (var prop in filtersObj.Properties())
                {
                    var groupList = new List<FilterGroup>();
                    var arr = prop.Value as JArray;
                    if (arr != null)
                    {
                        foreach (var g in arr)
                        {
                            var fg = new FilterGroup
                            {
                                Key = g["key"]?.ToString() ?? "",
                                Name = g["name"]?.ToString() ?? "",
                            };
                            var valArr = g["value"] as JArray;
                            if (valArr != null)
                            {
                                foreach (var v in valArr)
                                {
                                    fg.Value.Add(new FilterValueItem
                                    {
                                        N = v["n"]?.ToString() ?? "",
                                        V = v["v"]?.ToString() ?? "",
                                    });
                                }
                            }
                            groupList.Add(fg);
                        }
                    }
                    result.Filters[prop.Name] = groupList;
                }
            }

            return result;
        }
        catch { return new CategoryResult(); }
    }

    private static JObject? TryParseJson(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return null;
        try
        {
            return JObject.Parse(str);
        }
        catch
        {
            return null;
        }
    }
}
