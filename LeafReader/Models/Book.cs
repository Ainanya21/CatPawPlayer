namespace LeafReader.Models;

public class Book
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = "未知作者";
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = ".txt";
    public long FileSizeBytes { get; set; }
    public double ReadingProgress { get; set; }
    public double ScrollOffset { get; set; }
    public int LastReadCharacterOffset { get; set; }
    public DateTime LastReadTime { get; set; } = DateTime.Now;
    public DateTime AddedTime { get; set; } = DateTime.Now;
    public List<Bookmark> Bookmarks { get; set; } = new();
}
