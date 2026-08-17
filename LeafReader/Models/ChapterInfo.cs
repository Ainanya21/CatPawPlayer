namespace LeafReader.Models;

public sealed class ChapterInfo
{
    public required string Title { get; init; }
    public required int Offset { get; init; }

    public override string ToString() => Title;
}
