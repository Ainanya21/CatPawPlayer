namespace LeafReader.Models;

public class Bookmark
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int CharacterOffset { get; set; }
    public double Progress { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string PreviewText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
