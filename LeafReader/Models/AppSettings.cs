namespace LeafReader.Models;

public class AppSettings
{
    public double WindowWidth { get; set; } = 1180;
    public double WindowHeight { get; set; } = 780;
    public bool IsWindowMaximized { get; set; }
    public double FontSize { get; set; } = 19;
    public double LineHeightRatio { get; set; } = 1.75;
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
    public string ReadingTheme { get; set; } = "paper";
    public int ReadingColumns { get; set; } = 2;
    public string PageTurnEffect { get; set; } = "smooth";
    public string ReadingMode { get; set; } = "paged";
    public int VoiceRate { get; set; } = 0;
    public int VoiceVolume { get; set; } = 100;
}
