using System.IO;
using System.Text.Json;
using LeafReader.Models;

namespace LeafReader.Services;

public class LibraryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _appDataDir;
    private readonly string _booksJsonPath;
    private readonly string _settingsJsonPath;

    public List<Book> Books { get; private set; } = new();
    public AppSettings Settings { get; private set; } = new();

    public LibraryService()
    {
        _appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LeafReader");
        _booksJsonPath = Path.Combine(_appDataDir, "library.json");
        _settingsJsonPath = Path.Combine(_appDataDir, "settings.json");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_appDataDir);
        await LoadSettingsAsync();
        await LoadBooksAsync();
    }

    public async Task SaveSettingsAsync()
    {
        Directory.CreateDirectory(_appDataDir);
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsJsonPath, json);
    }

    public async Task SaveBooksAsync()
    {
        Directory.CreateDirectory(_appDataDir);
        var json = JsonSerializer.Serialize(Books, JsonOptions);
        await File.WriteAllTextAsync(_booksJsonPath, json);
    }

    public async Task<Book> ImportBookAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) throw new FileNotFoundException("未找到待导入的文件", sourceFilePath);

        var ext = Path.GetExtension(sourceFilePath).ToLower();
        var existing = Books.FirstOrDefault(b => b.FilePath.Equals(sourceFilePath, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return existing;

        var book = new Book
        {
            FilePath = sourceFilePath,
            FileType = ext,
            FileSizeBytes = new FileInfo(sourceFilePath).Length,
            AddedTime = DateTime.Now,
            LastReadTime = DateTime.Now
        };

        if (ext == ".epub")
        {
            var epubService = new EpubService();
            var (title, author, _) = epubService.ExtractTextAndMetadata(sourceFilePath);
            book.Title = title;
            book.Author = author;
        }
        else
        {
            book.Title = Path.GetFileNameWithoutExtension(sourceFilePath);
        }

        Books.Insert(0, book);
        await SaveBooksAsync();
        return book;
    }

    public async Task SaveProgressAsync(Book book, double scrollOffset, double progress, int characterOffset)
    {
        book.ScrollOffset = Math.Max(0, scrollOffset);
        book.ReadingProgress = Math.Clamp(progress, 0, 1);
        book.LastReadCharacterOffset = Math.Max(0, characterOffset);
        book.LastReadTime = DateTime.Now;
        await SaveBooksAsync();
    }

    public async Task AddBookmarkAsync(Book book, Bookmark bookmark)
    {
        book.Bookmarks.Add(bookmark);
        await SaveBooksAsync();
    }

    public async Task RemoveBookmarkAsync(Book book, Bookmark bookmark)
    {
        book.Bookmarks.Remove(bookmark);
        await SaveBooksAsync();
    }

    public async Task DeleteBookAsync(Book book)
    {
        Books.Remove(book);
        await SaveBooksAsync();
    }

    private async Task LoadSettingsAsync()
    {
        if (!File.Exists(_settingsJsonPath))
        {
            Settings = new AppSettings();
            await SaveSettingsAsync();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsJsonPath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    private async Task LoadBooksAsync()
    {
        if (!File.Exists(_booksJsonPath))
        {
            Books = new List<Book>();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_booksJsonPath);
            Books = JsonSerializer.Deserialize<List<Book>>(json, JsonOptions) ?? new List<Book>();
            Books = Books.Where(b => File.Exists(b.FilePath)).ToList();
        }
        catch
        {
            Books = new List<Book>();
        }
    }
}
