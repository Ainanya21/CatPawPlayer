using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LeafReader.Services;

public class EpubService
{
    public (string Title, string Author, string RawText) ExtractTextAndMetadata(string epubFilePath)
    {
        using var archive = ZipFile.OpenRead(epubFilePath);
        var containerEntry = archive.GetEntry("META-INF/container.xml")
            ?? throw new InvalidDataException("无效的 EPUB 文件：缺失 META-INF/container.xml");

        XDocument containerXml;
        using (var stream = containerEntry.Open())
        {
            containerXml = XDocument.Load(stream);
        }

        var opfPath = containerXml.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "rootfile")
            ?.Attribute("full-path")?.Value
            ?? throw new InvalidDataException("无效的 EPUB 文件：无法获取 OPF 路径");

        var opfEntry = archive.GetEntry(opfPath)
            ?? throw new InvalidDataException($"无效的 EPUB 文件：缺失 OPF 文件 {opfPath}");

        XDocument opfXml;
        using (var stream = opfEntry.Open())
        {
            opfXml = XDocument.Load(stream);
        }

        string title = opfXml.Descendants().FirstOrDefault(e => e.Name.LocalName == "title")?.Value ?? Path.GetFileNameWithoutExtension(epubFilePath);
        string author = opfXml.Descendants().FirstOrDefault(e => e.Name.LocalName == "creator")?.Value ?? "未知作者";

        var opfDir = Path.GetDirectoryName(opfPath)?.Replace('\\', '/') ?? string.Empty;

        var manifestItems = opfXml.Descendants()
            .Where(e => e.Name.LocalName == "item")
            .ToDictionary(
                e => e.Attribute("id")?.Value ?? string.Empty,
                e => e.Attribute("href")?.Value ?? string.Empty);

        var spineItemRefs = opfXml.Descendants()
            .Where(e => e.Name.LocalName == "itemref")
            .Select(e => e.Attribute("idref")?.Value ?? string.Empty)
            .ToList();

        var sb = new StringBuilder();

        foreach (var idref in spineItemRefs)
        {
            if (!manifestItems.TryGetValue(idref, out var href)) continue;
            var fullHtmlPath = string.IsNullOrEmpty(opfDir) ? href : $"{opfDir}/{href}";
            var htmlEntry = archive.GetEntry(fullHtmlPath) ?? archive.Entries.FirstOrDefault(e => e.FullName.Equals(fullHtmlPath, StringComparison.OrdinalIgnoreCase));

            if (htmlEntry == null) continue;

            using var htmlStream = htmlEntry.Open();
            using var reader = new StreamReader(htmlStream, Encoding.UTF8);
            var htmlContent = reader.ReadToEnd();

            var text = StripHtmlTags(htmlContent);
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
                sb.AppendLine();
            }
        }

        return (title, author, sb.ToString());
    }

    private static string StripHtmlTags(string html)
    {
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</p>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</div>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", "", RegexOptions.IgnoreCase);

        var decoded = System.Net.WebUtility.HtmlDecode(html);
        var lines = decoded.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
        return string.Join("\n\n", lines);
    }
}
