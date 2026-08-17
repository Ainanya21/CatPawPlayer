using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using LeafReader.Helpers;
using LeafReader.Models;
using LeafReader.Services;
using Microsoft.Win32;

namespace LeafReader;

public partial class MainWindow : Window
{
    private const int ReaderChunkSize = 24_000;
    private static readonly Regex ChapterTitlePattern = new(
        @"^(?:第[0-9０-９零〇一二三四五六七八九十百千万两]+[章节卷回部篇](?:\s+|[：:、.．-])?.{0,40}|序章|楔子|前言|后记|尾声|终章|大结局|番外(?:.{0,30})?)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly LibraryService _library = new();
    private readonly TtsService _ttsService = new();
    private readonly DispatcherTimer _progressTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private readonly DispatcherTimer _settingsTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly Stack<int> _pageHistory = new();

    private Book? _currentBook;
    private bool _isRestoringPosition;
    private bool _recentOnly;
    private bool _isLoaded;
    private bool _isClosing;
    private int _findIndex = -1;
    private string _currentText = string.Empty;
    private int _readerChunkStart;
    private int _readerChunkLength;
    private int _readerFirstColumnLength;
    private int _readingColumns = 2;
    private bool _isUpdatingChapterSelection;
    private bool _isPageTransitioning;
    private string _readingMode = "paged";
    private string _pageTurnEffect = "smooth";

    public ObservableCollection<Book> DisplayBooks { get; } = new();
    public ObservableCollection<ChapterInfo> Chapters { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        _progressTimer.Tick += ProgressTimer_Tick;
        _settingsTimer.Tick += SettingsTimer_Tick;
        _ttsService.StateChanged += TtsService_StateChanged;
        ReadingArea.SizeChanged += ReadingArea_SizeChanged;
    }

    private void ReadingArea_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isLoaded && _currentBook is not null && _readingMode == "paged" && !_isPageTransitioning)
        {
            _ = ShowReaderPageAsync(_readerChunkStart, true);
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            WindowBackdropHelper.ApplyBackdrop(this, BackdropType.Mica);
            await _library.InitializeAsync();
            ApplySavedSettings();
            _isLoaded = true;
            RefreshBooks();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "无法打开书库", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isClosing) return;
        e.Cancel = true;
        _isClosing = true;
        _progressTimer.Stop();
        _settingsTimer.Stop();
        _ttsService.Stop();
        _ttsService.Dispose();
        try
        {
            await SaveCurrentProgressAsync();
            if (WindowState == WindowState.Normal)
            {
                _library.Settings.WindowWidth = ActualWidth;
                _library.Settings.WindowHeight = ActualHeight;
            }
            _library.Settings.IsWindowMaximized = WindowState == WindowState.Maximized;
            await _library.SaveSettingsAsync();
        }
        finally
        {
            Application.Current.Shutdown();
        }
    }

    private void RefreshBooks()
    {
        DisplayBooks.Clear();
        var query = _library.Books.AsEnumerable();
        if (_recentOnly)
        {
            query = query.OrderByDescending(b => b.LastReadTime);
        }
        else
        {
            query = query.OrderByDescending(b => b.AddedTime);
        }

        foreach (var book in query)
        {
            DisplayBooks.Add(book);
        }

        EmptyLibraryState.Visibility = DisplayBooks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        LibraryCountText.Text = $"共 {DisplayBooks.Count} 本图书";
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要导入的电子书",
            Filter = "支持的书籍格式 (*.txt;*.epub)|*.txt;*.epub|文本文件 (*.txt)|*.txt|EPUB 电子书 (*.epub)|*.epub|所有文件 (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            Book? firstImported = null;
            foreach (var file in dialog.FileNames)
            {
                try
                {
                    var book = await _library.ImportBookAsync(file);
                    firstImported ??= book;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"导入失败: {Path.GetFileName(file)}\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            RefreshBooks();
            if (firstImported != null)
            {
                await OpenBookAsync(firstImported);
            }
        }
    }

    private void RecentFilterButton_Click(object sender, RoutedEventArgs e)
    {
        _recentOnly = !_recentOnly;
        RecentFilterButton.BorderThickness = _recentOnly ? new Thickness(1) : new Thickness(0);
        RecentFilterButton.BorderBrush = _recentOnly ? (Brush)FindResource("AccentBrush") : Brushes.Transparent;
        RefreshBooks();
    }

    private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var current = _library.Settings.ReadingTheme;
        var next = current switch
        {
            "paper" => "light",
            "light" => "green",
            "green" => "dark",
            _ => "paper"
        };
        ApplyReadingTheme(next, true);
    }

    private async void OpenBook_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Book book })
        {
            await OpenBookAsync(book);
        }
    }

    private async void DeleteBook_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Book book })
        {
            var result = MessageBox.Show(this, $"确定要将《{book.Title}》从书库移除吗？\n（物理文件不会被删除）", "移除图书", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _library.DeleteBookAsync(book);
                RefreshBooks();
            }
        }
    }

    private async Task OpenBookAsync(Book book)
    {
        try
        {
            _pageHistory.Clear();
            if (book.FileType.Equals(".epub", StringComparison.OrdinalIgnoreCase))
            {
                var epubService = new EpubService();
                var (_, _, rawText) = epubService.ExtractTextAndMetadata(book.FilePath);
                _currentText = rawText;
            }
            else
            {
                _currentText = await File.ReadAllTextAsync(book.FilePath);
            }

            _currentBook = book;
            ReaderTitle.Text = book.Title;
            LibraryView.Visibility = Visibility.Collapsed;
            ReaderView.Visibility = Visibility.Visible;
            ChaptersPanel.Visibility = Visibility.Collapsed;
            BookmarksPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            FindBar.Visibility = Visibility.Collapsed;

            var chapters = ParseChapters(_currentText);
            Chapters.Clear();
            foreach (var chapter in chapters) Chapters.Add(chapter);
            ChapterSummary.Text = $"共 {Chapters.Count} 章";

            RefreshBookmarksList();

            int targetOffset = book.LastReadCharacterOffset;
            if (targetOffset <= 0 && book.ReadingProgress > 0)
            {
                targetOffset = (int)(_currentText.Length * book.ReadingProgress);
            }

            if (_readingMode == "scroll")
            {
                await ShowReaderChunkAsync(targetOffset, book.ScrollOffset);
            }
            else
            {
                await ShowReaderPageAsync(targetOffset, true);
            }

            UpdateReaderStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"打开书籍失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BackToLibrary_Click(object sender, RoutedEventArgs e)
    {
        _ttsService.Stop();
        await SaveCurrentProgressAsync();
        ReaderView.Visibility = Visibility.Collapsed;
        LibraryView.Visibility = Visibility.Visible;
        RefreshBooks();
    }

    private void ReaderScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isRestoringPosition || _currentBook is null) return;
        UpdateReaderStatus();
        _progressTimer.Stop();
        _progressTimer.Start();
    }

    private async void ProgressTimer_Tick(object? sender, EventArgs e)
    {
        _progressTimer.Stop();
        await SaveCurrentProgressAsync();
    }

    private async Task SaveCurrentProgressAsync()
    {
        if (_currentBook is null) return;
        var characterOffset = GetCurrentCharacterOffset();
        var progress = _currentText.Length <= 0 ? 0 : (double)characterOffset / _currentText.Length;
        var scrollOffset = _readingMode == "scroll" ? ReaderScrollViewer.VerticalOffset : 0;
        await _library.SaveProgressAsync(
            _currentBook, scrollOffset, progress, characterOffset);
    }

    private void UpdateReaderStatus()
    {
        var characterOffset = GetCurrentCharacterOffset();
        var progress = _currentText.Length <= 0 ? 0 : (double)characterOffset / _currentText.Length;
        ReaderStatus.Text = $"{progress:P0}  ·  {characterOffset:N0} / {_currentText.Length:N0} 字符";
        if (Chapters.Count > 0)
        {
            UpdateSelectedChapter(characterOffset);
        }
    }

    private int GetCurrentCharacterOffset()
    {
        if (string.IsNullOrEmpty(_currentText) || _readerChunkLength <= 0) return 0;
        if (_readingMode == "scroll")
        {
            var maxScroll = Math.Max(1, ReaderScrollViewer.ScrollableHeight);
            var ratio = Math.Clamp(ReaderScrollViewer.VerticalOffset / maxScroll, 0, 1);
            return Math.Clamp(_readerChunkStart + (int)(_readerChunkLength * ratio), 0, _currentText.Length);
        }
        return Math.Clamp(_readerChunkStart, 0, _currentText.Length);
    }

    private async Task ShowReaderPageAsync(int characterOffset, bool isJump = true)
    {
        if (isJump) _pageHistory.Clear();
        var alignedOffset = Math.Clamp(characterOffset, 0, Math.Max(0, _currentText.Length - 1));
        await ShowReaderChunkAsync(alignedOffset);
    }

    private (string Col1Text, string Col2Text, int TotalChars) MeasureAndFitPage(int startOffset)
    {
        if (string.IsNullOrEmpty(_currentText) || startOffset >= _currentText.Length)
        {
            return (string.Empty, string.Empty, 0);
        }

        var start = Math.Clamp(startOffset, 0, _currentText.Length - 1);

        var fontSize = ReaderTextBlock.FontSize;
        var lineHeight = ReaderTextBlock.LineHeight > 0 ? ReaderTextBlock.LineHeight : fontSize * 1.75;
        var availHeight = Math.Max(100, ReadingArea.ActualHeight - 60);
        var maxLines = Math.Max(1, (int)Math.Floor(availHeight / lineHeight));
        var targetHeight = maxLines * lineHeight + 1.5;

        var totalAvailWidth = Math.Max(200, (ReadingArea.ActualWidth > 0 ? Math.Min(ReaderPageContainer.MaxWidth, ReadingArea.ActualWidth) : 1100) - 80);
        var colWidth = _readingColumns == 2 ? (totalAvailWidth - 49) / 2 : totalAvailWidth;

        // --- Column 1 ---
        int maxLimit1 = _currentText.Length - start;
        for (int i = 0; i < Chapters.Count; i++)
        {
            var chapOffset = Chapters[i].Offset;
            if (chapOffset > start)
            {
                maxLimit1 = Math.Min(maxLimit1, chapOffset - start);
                break;
            }
        }

        if (maxLimit1 <= 0)
        {
            return (string.Empty, string.Empty, 0);
        }

        int len1 = MeasureColumnText(start, maxLimit1, colWidth, targetHeight, lineHeight, fontSize);
        string col1Text = _currentText.Substring(start, len1);

        // --- Column 2 (if double-column mode) ---
        int len2 = 0;
        string col2Text = string.Empty;

        int start2 = start + len1;
        if (_readingColumns == 2 && start2 < _currentText.Length)
        {
            int maxLimit2 = _currentText.Length - start2;
            for (int i = 0; i < Chapters.Count; i++)
            {
                var chapOffset = Chapters[i].Offset;
                if (chapOffset > start2)
                {
                    maxLimit2 = Math.Min(maxLimit2, chapOffset - start2);
                    break;
                }
            }

            if (maxLimit2 > 0)
            {
                len2 = MeasureColumnText(start2, maxLimit2, colWidth, targetHeight, lineHeight, fontSize);
                col2Text = _currentText.Substring(start2, len2);
            }
        }

        return (col1Text, col2Text, len1 + len2);
    }

    private int MeasureColumnText(int start, int maxChars, double colWidth, double targetHeight, double lineHeight, double fontSize)
    {
        if (maxChars <= 0) return 0;

        int low = 1;
        int high = Math.Min(maxChars, 4000);
        int best = 1;

        var typeface = new Typeface(ReaderTextBlock.FontFamily, ReaderTextBlock.FontStyle, ReaderTextBlock.FontWeight, ReaderTextBlock.FontStretch);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var brush = ReaderTextBlock.Foreground;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            var sub = _currentText.Substring(start, mid);
            var ft = new FormattedText(
                sub, culture, FlowDirection.LeftToRight,
                typeface, fontSize, brush, dpi)
            {
                MaxTextWidth = Math.Max(10, colWidth),
                LineHeight = lineHeight
            };

            if (ft.Height <= targetHeight)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
    }

    private int CalculatePreviousPageStart(int currentStart)
    {
        if (currentStart <= 0) return 0;

        int minAllowedStart = 0;
        for (int i = Chapters.Count - 1; i >= 0; i--)
        {
            if (Chapters[i].Offset < currentStart)
            {
                minAllowedStart = Chapters[i].Offset;
                break;
            }
        }

        if (currentStart <= minAllowedStart) return minAllowedStart;

        int searchMax = currentStart - 1;
        int searchMin = Math.Max(minAllowedStart, currentStart - 3500);

        int low = searchMin;
        int high = searchMax;
        int bestStart = searchMin;
        int minDiff = int.MaxValue;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            var page = MeasureAndFitPage(mid);
            int pageEnd = mid + page.TotalChars;

            int diff = Math.Abs(pageEnd - currentStart);
            if (diff < minDiff)
            {
                minDiff = diff;
                bestStart = mid;
            }

            if (pageEnd == currentStart)
            {
                return mid;
            }
            else if (pageEnd < currentStart)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return bestStart;
    }

    private async Task ShowReaderChunkAsync(int startCharacterOffset, double restoreScrollOffset = 0)
    {
        if (string.IsNullOrEmpty(_currentText)) return;

        int start = Math.Clamp(startCharacterOffset, 0, Math.Max(0, _currentText.Length - 1));
        int length;

        if (_readingMode == "scroll")
        {
            length = Math.Min(ReaderChunkSize, _currentText.Length - start);
            var chunkText = _currentText.Substring(start, length);
            if (_readingColumns == 1)
            {
                ReaderTextBlock.Text = chunkText;
                ReaderTextBlockSecond.Text = string.Empty;
                _readerFirstColumnLength = chunkText.Length;
            }
            else
            {
                var splitIndex = chunkText.Length / 2;
                var spaceIndex = chunkText.LastIndexOf('\n', splitIndex);
                if (spaceIndex > chunkText.Length / 4) splitIndex = spaceIndex;
                ReaderTextBlock.Text = chunkText[..splitIndex];
                ReaderTextBlockSecond.Text = chunkText[splitIndex..];
                _readerFirstColumnLength = splitIndex;
            }
        }
        else
        {
            var page = MeasureAndFitPage(start);
            length = page.TotalChars;
            if (length <= 0) length = 1;
            ReaderTextBlock.Text = page.Col1Text;
            ReaderTextBlockSecond.Text = page.Col2Text;
            _readerFirstColumnLength = page.Col1Text.Length;
        }

        _readerChunkStart = start;
        _readerChunkLength = length;

        PreviousChunkButton.IsEnabled = start > 0;
        NextChunkButton.IsEnabled = start + length < _currentText.Length;

        _isRestoringPosition = true;
        ReaderScrollViewer.ScrollToVerticalOffset(restoreScrollOffset);
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        _isRestoringPosition = false;

        UpdateReaderPageStatus();
        UpdateReaderStatus();
    }

    private void UpdateReaderPageStatus()
    {
        if (_currentText.Length == 0 || _readerChunkLength == 0)
        {
            ReaderPageStatus.Text = string.Empty;
            return;
        }
        if (_readingMode == "scroll")
        {
            var currentChunkIndex = (_readerChunkStart / ReaderChunkSize) + 1;
            var totalChunks = (int)Math.Ceiling((double)_currentText.Length / ReaderChunkSize);
            ReaderPageStatus.Text = $"段落 {currentChunkIndex} / {totalChunks}";
        }
        else
        {
            var percent = (double)(_readerChunkStart + _readerChunkLength) / _currentText.Length;
            ReaderPageStatus.Text = $"{percent:P1}";
        }
    }

    private async void PreviousChunkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPageTransitioning || _readerChunkStart <= 0) return;
        int newStart;
        if (_readingMode == "paged")
        {
            if (_pageHistory.Count > 0)
            {
                newStart = _pageHistory.Pop();
            }
            else
            {
                newStart = CalculatePreviousPageStart(_readerChunkStart);
            }
        }
        else
        {
            newStart = Math.Max(0, _readerChunkStart - ReaderChunkSize);
        }

        if (_readingMode == "paged")
        {
            _isPageTransitioning = true;
            try
            {
                if (_pageTurnEffect == "realistic")
                    await RunRealisticPageTurnAsync(newStart, -1);
                else
                    await RunSmoothPageTurnAsync(newStart, -1);
            }
            finally
            {
                _isPageTransitioning = false;
                ResetPageAnimations();
                PageTurnOverlay.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            await ShowReaderChunkAsync(newStart);
        }
    }

    private async void NextChunkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPageTransitioning || _readerChunkStart + _readerChunkLength >= _currentText.Length) return;
        if (_readingMode == "paged")
        {
            _pageHistory.Push(_readerChunkStart);
        }
        var newStart = _readerChunkStart + _readerChunkLength;
        if (_readingMode == "paged")
        {
            _isPageTransitioning = true;
            try
            {
                if (_pageTurnEffect == "realistic")
                    await RunRealisticPageTurnAsync(newStart, 1);
                else
                    await RunSmoothPageTurnAsync(newStart, 1);
            }
            finally
            {
                _isPageTransitioning = false;
                ResetPageAnimations();
                PageTurnOverlay.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            await ShowReaderChunkAsync(newStart);
        }
    }

    private void ReaderSurface_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_readingMode == "scroll" || _currentBook is null || _isPageTransitioning) return;

        if (IsMouseOverElement(ChaptersPanel) ||
            IsMouseOverElement(BookmarksPanel) ||
            IsMouseOverElement(SettingsPanel) ||
            IsMouseOverElement(FindBar) ||
            IsMouseOverElement(TtsBar))
        {
            return;
        }

        if (e.Delta < 0)
        {
            NextChunkButton_Click(sender, e);
        }
        else if (e.Delta > 0)
        {
            PreviousChunkButton_Click(sender, e);
        }
        e.Handled = true;
    }

    private static bool IsMouseOverElement(UIElement element)
    {
        if (element == null || element.Visibility != Visibility.Visible) return false;
        if (element is FrameworkElement fe && fe.IsMouseOver) return true;
        return false;
    }

    private async Task RunSmoothPageTurnAsync(int targetStart, int direction)
    {
        var distance = Math.Max(1, ReadingArea.ActualWidth);
        PageTurnSnapshot.Source = CaptureReadingViewport();
        PageTurnEdgeShadow.Visibility = Visibility.Collapsed;
        PageTurnSheet.RenderTransformOrigin = new Point(0.5, 0.5);
        PageTurnScale.ScaleX = 1;
        PageTurnSkew.AngleY = 0;
        PageTurnRotate.Angle = 0;
        PageTurnSheetTranslate.X = 0;
        PageTurnSheet.Opacity = 1;
        PageTurnOverlay.Visibility = Visibility.Visible;

        ReaderViewportTranslate.X = direction > 0 ? distance : -distance;
        await ShowReaderChunkAsync(targetStart);
        var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };
        await Task.WhenAll(
            AnimateAsync(PageTurnSheetTranslate, TranslateTransform.XProperty, 0,
                direction > 0 ? -distance : distance, 360, easing),
            AnimateAsync(ReaderViewportTranslate, TranslateTransform.XProperty,
                ReaderViewportTranslate.X, 0, 360, easing));
    }

    private async Task RunRealisticPageTurnAsync(int targetStart, int direction)
    {
        var snapshotOld = CaptureReadingViewport();
        var viewportWidth = Math.Max(1, ReadingArea.ActualWidth);

        if (_readingColumns == 2 && viewportWidth > 200)
        {
            var halfWidth = viewportWidth / 2;
            int pixelWidth = snapshotOld.PixelWidth;
            int pixelHeight = snapshotOld.PixelHeight;
            int halfPixelWidth = pixelWidth / 2;

            CroppedBitmap oldLeftCrop = new CroppedBitmap(snapshotOld, new Int32Rect(0, 0, halfPixelWidth, pixelHeight));
            CroppedBitmap oldRightCrop = new CroppedBitmap(snapshotOld, new Int32Rect(halfPixelWidth, 0, pixelWidth - halfPixelWidth, pixelHeight));

            PageTurnStaticSheet.Width = halfWidth;
            PageTurnStaticSheet.Visibility = Visibility.Visible;
            PageTurnSheet.Width = halfWidth;

            await ShowReaderChunkAsync(targetStart);
            var snapshotNew = CaptureReadingViewport();
            CroppedBitmap newLeftCrop = new CroppedBitmap(snapshotNew, new Int32Rect(0, 0, halfPixelWidth, pixelHeight));
            CroppedBitmap newRightCrop = new CroppedBitmap(snapshotNew, new Int32Rect(halfPixelWidth, 0, pixelWidth - halfPixelWidth, pixelHeight));

            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

            if (direction > 0)
            {
                PageTurnStaticSheet.HorizontalAlignment = HorizontalAlignment.Left;
                PageTurnStaticSnapshot.Source = oldLeftCrop;

                PageTurnSheet.HorizontalAlignment = HorizontalAlignment.Right;
                PageTurnSheet.RenderTransformOrigin = new Point(0, 0.5);
                PageTurnSnapshot.Source = oldRightCrop;
                PageTurnEdgeShadow.HorizontalAlignment = HorizontalAlignment.Right;
                PageTurnEdgeShadow.Visibility = Visibility.Visible;

                PageTurnScale.ScaleX = 1;
                PageTurnSkew.AngleY = 0;
                PageTurnRotate.Angle = 0;
                PageTurnOverlay.Visibility = Visibility.Visible;

                await Task.WhenAll(
                    AnimateAsync(PageTurnScale, ScaleTransform.ScaleXProperty, 1.0, 0.02, 210, ease),
                    AnimateAsync(PageTurnSkew, SkewTransform.AngleYProperty, 0, -6.0, 210, ease),
                    AnimateAsync(PageTurnRotate, RotateTransform.AngleProperty, 0, -1.2, 210, ease));

                PageTurnSheet.HorizontalAlignment = HorizontalAlignment.Left;
                PageTurnSheet.RenderTransformOrigin = new Point(1, 0.5);
                PageTurnSnapshot.Source = newLeftCrop;
                PageTurnEdgeShadow.HorizontalAlignment = HorizontalAlignment.Left;

                await Task.WhenAll(
                    AnimateAsync(PageTurnScale, ScaleTransform.ScaleXProperty, 0.02, 1.0, 210, ease),
                    AnimateAsync(PageTurnSkew, SkewTransform.AngleYProperty, 6.0, 0, 210, ease),
                    AnimateAsync(PageTurnRotate, RotateTransform.AngleProperty, 1.2, 0, 210, ease));
            }
            else
            {
                PageTurnStaticSheet.HorizontalAlignment = HorizontalAlignment.Right;
                PageTurnStaticSnapshot.Source = oldRightCrop;

                PageTurnSheet.HorizontalAlignment = HorizontalAlignment.Left;
                PageTurnSheet.RenderTransformOrigin = new Point(1, 0.5);
                PageTurnSnapshot.Source = oldLeftCrop;
                PageTurnEdgeShadow.HorizontalAlignment = HorizontalAlignment.Left;
                PageTurnEdgeShadow.Visibility = Visibility.Visible;

                PageTurnScale.ScaleX = 1;
                PageTurnSkew.AngleY = 0;
                PageTurnRotate.Angle = 0;
                PageTurnOverlay.Visibility = Visibility.Visible;

                await Task.WhenAll(
                    AnimateAsync(PageTurnScale, ScaleTransform.ScaleXProperty, 1.0, 0.02, 210, ease),
                    AnimateAsync(PageTurnSkew, SkewTransform.AngleYProperty, 0, 6.0, 210, ease),
                    AnimateAsync(PageTurnRotate, RotateTransform.AngleProperty, 0, 1.2, 210, ease));

                PageTurnSheet.HorizontalAlignment = HorizontalAlignment.Right;
                PageTurnSheet.RenderTransformOrigin = new Point(0, 0.5);
                PageTurnSnapshot.Source = newRightCrop;
                PageTurnEdgeShadow.HorizontalAlignment = HorizontalAlignment.Right;

                await Task.WhenAll(
                    AnimateAsync(PageTurnScale, ScaleTransform.ScaleXProperty, 0.02, 1.0, 210, ease),
                    AnimateAsync(PageTurnSkew, SkewTransform.AngleYProperty, -6.0, 0, 210, ease),
                    AnimateAsync(PageTurnRotate, RotateTransform.AngleProperty, -1.2, 0, 210, ease));
            }
        }
        else
        {
            PageTurnStaticSheet.Visibility = Visibility.Collapsed;
            PageTurnSheet.Width = double.NaN;
            PageTurnSheet.HorizontalAlignment = HorizontalAlignment.Stretch;
            PageTurnSnapshot.Source = snapshotOld;
            PageTurnSheet.RenderTransformOrigin = direction > 0 ? new Point(0, 0.5) : new Point(1, 0.5);
            PageTurnEdgeShadow.HorizontalAlignment = direction > 0 ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            PageTurnEdgeShadow.Visibility = Visibility.Visible;
            PageTurnScale.ScaleX = 1;
            PageTurnSkew.AngleY = 0;
            PageTurnRotate.Angle = 0;
            PageTurnOverlay.Visibility = Visibility.Visible;

            await ShowReaderChunkAsync(targetStart);
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            await Task.WhenAll(
                AnimateAsync(PageTurnScale, ScaleTransform.ScaleXProperty, 1, 0.035, 420, ease),
                AnimateAsync(PageTurnSkew, SkewTransform.AngleYProperty, 0, direction > 0 ? -6.5 : 6.5, 420, ease),
                AnimateAsync(PageTurnRotate, RotateTransform.AngleProperty, 0, direction > 0 ? -1.5 : 1.5, 420, ease),
                AnimateAsync(PageTurnSheet, UIElement.OpacityProperty, 1, 0.85, 420, ease));
        }
    }

    private RenderTargetBitmap CaptureReadingViewport()
    {
        ReaderScrollViewer.UpdateLayout();
        var sourceWidth = Math.Max(1, ReaderScrollViewer.ActualWidth);
        var sourceHeight = Math.Max(1, ReaderScrollViewer.ActualHeight);
        var scale = Math.Min(1, Math.Min(1_400 / sourceWidth, 900 / sourceHeight));
        var width = Math.Max(1, (int)Math.Ceiling(sourceWidth * scale));
        var height = Math.Max(1, (int)Math.Ceiling(sourceHeight * scale));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var brush = new VisualBrush(ReaderScrollViewer)
            {
                Stretch = Stretch.Fill,
                Viewbox = new Rect(0, 0, sourceWidth, sourceHeight),
                ViewboxUnits = BrushMappingMode.Absolute
            };
            context.DrawRectangle(brush, null, new Rect(0, 0, width, height));
        }
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static Task AnimateAsync(
        DependencyObject target, DependencyProperty property, double from, double to,
        int milliseconds, IEasingFunction easing)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd
        };
        animation.Completed += (_, _) => completion.TrySetResult();
        switch (target)
        {
            case UIElement element:
                element.BeginAnimation(property, animation);
                break;
            case Animatable animatable:
                animatable.BeginAnimation(property, animation);
                break;
        }
        return completion.Task;
    }

    private void ResetPageAnimations()
    {
        ReaderPageContainer.BeginAnimation(UIElement.OpacityProperty, null);
        ReaderPageTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        ReaderViewportTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        PageTurnScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        PageTurnSkew.BeginAnimation(SkewTransform.AngleYProperty, null);
        PageTurnRotate.Angle = 0;
        PageTurnSheetTranslate.X = 0;
        PageTurnSheet.BeginAnimation(UIElement.OpacityProperty, null);
        ReaderPageContainer.Opacity = 1;
        ReaderPageTranslate.X = 0;
        ReaderViewportTranslate.X = 0;
        PageTurnSheetTranslate.X = 0;
        PageTurnSheet.Width = double.NaN;
        PageTurnSheet.HorizontalAlignment = HorizontalAlignment.Stretch;
        PageTurnStaticSheet.Visibility = Visibility.Collapsed;
        PageTurnStaticSnapshot.Source = null;
    }

    private static List<ChapterInfo> ParseChapters(string text)
    {
        var chapters = new List<ChapterInfo>();
        var lineStart = 0;
        while (lineStart < text.Length && chapters.Count < 5_000)
        {
            var lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd == -1) lineEnd = text.Length;

            var line = text[lineStart..lineEnd].Trim();
            if (line.Length is >= 2 and <= 45 && ChapterTitlePattern.IsMatch(line))
            {
                chapters.Add(new ChapterInfo
                {
                    Title = line,
                    Offset = lineStart
                });
            }

            lineStart = lineEnd + 1;
        }
        return chapters;
    }

    private void ToggleChapters_Click(object sender, RoutedEventArgs e)
    {
        BookmarksPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        FindBar.Visibility = Visibility.Collapsed;
        ChaptersPanel.Visibility = ChaptersPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void ChaptersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingChapterSelection || ChaptersList.SelectedItem is not ChapterInfo chapter) return;
        ChaptersPanel.Visibility = Visibility.Collapsed;
        await ShowReaderPageAsync(chapter.Offset, true);
        UpdateReaderStatus();
    }

    private void ToggleBookmarks_Click(object sender, RoutedEventArgs e)
    {
        ChaptersPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        FindBar.Visibility = Visibility.Collapsed;
        BookmarksPanel.Visibility = BookmarksPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CloseBookmarks_Click(object sender, RoutedEventArgs e)
    {
        BookmarksPanel.Visibility = Visibility.Collapsed;
    }

    private async void AddBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (_currentBook is null || string.IsNullOrEmpty(_currentText)) return;
        var offset = GetCurrentCharacterOffset();

        var previewLength = Math.Min(35, _currentText.Length - offset);
        var preview = _currentText.Substring(offset, Math.Max(0, previewLength)).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (string.IsNullOrWhiteSpace(preview)) preview = "书签位置";

        var chapterTitle = ChaptersList.SelectedItem is ChapterInfo c ? c.Title : "正文";
        var progress = (double)offset / _currentText.Length;

        var bookmark = new Bookmark
        {
            CharacterOffset = offset,
            Progress = progress,
            ChapterTitle = chapterTitle,
            PreviewText = preview
        };

        await _library.AddBookmarkAsync(_currentBook, bookmark);
        RefreshBookmarksList();
        ReaderStatus.Text = "已添加书签！";
    }

    private void RefreshBookmarksList()
    {
        if (_currentBook is null) return;
        BookmarksList.ItemsSource = null;
        BookmarksList.ItemsSource = _currentBook.Bookmarks.OrderByDescending(b => b.CreatedAt);
    }

    private async void BookmarksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BookmarksList.SelectedItem is Bookmark bookmark)
        {
            await NavigateToCharacterAsync(bookmark.CharacterOffset);
        }
    }

    private void ToggleTts_Click(object sender, RoutedEventArgs e)
    {
        TtsBar.Visibility = TtsBar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (TtsBar.Visibility == Visibility.Visible && !_ttsService.IsSpeaking)
        {
            StartReadingTts();
        }
    }

    private void StartReadingTts()
    {
        if (string.IsNullOrEmpty(_currentText)) return;
        var offset = GetCurrentCharacterOffset();
        var textToSpeak = _currentText.Substring(offset, Math.Min(3000, _currentText.Length - offset));
        _ttsService.Speak(textToSpeak);
    }

    private void TtsPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_ttsService.IsSpeaking)
        {
            if (_ttsService.IsPaused) _ttsService.Resume();
            else _ttsService.Pause();
        }
        else
        {
            StartReadingTts();
        }
    }

    private void TtsStop_Click(object sender, RoutedEventArgs e)
    {
        _ttsService.Stop();
    }

    private void TtsRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_ttsService is null) return;
        _ttsService.SetRate((int)e.NewValue);
    }

    private void CloseTts_Click(object sender, RoutedEventArgs e)
    {
        _ttsService.Stop();
        TtsBar.Visibility = Visibility.Collapsed;
    }

    private void TtsService_StateChanged(object? sender, bool isSpeaking)
    {
        Dispatcher.Invoke(() =>
        {
            TtsPlayPauseButton.Content = isSpeaking ? "\uE769" : "\uE768";
        });
    }

    private async Task NavigateToCharacterAsync(int characterOffset)
    {
        await ShowReaderPageAsync(characterOffset, true);
        UpdateReaderStatus();
    }

    private void UpdateSelectedChapter(int characterOffset)
    {
        if (Chapters.Count == 0) return;
        var low = 0;
        var high = Chapters.Count - 1;
        while (low <= high)
        {
            var mid = (low + high) / 2;
            var chapter = Chapters[mid];
            var nextOffset = mid + 1 < Chapters.Count ? Chapters[mid + 1].Offset : _currentText.Length;

            if (characterOffset >= chapter.Offset && characterOffset < nextOffset)
            {
                _isUpdatingChapterSelection = true;
                ChaptersList.SelectedItem = chapter;
                _isUpdatingChapterSelection = false;
                return;
            }

            if (characterOffset < chapter.Offset) high = mid - 1;
            else low = mid + 1;
        }
    }

    private void ToggleSettings_Click(object sender, RoutedEventArgs e)
    {
        ChaptersPanel.Visibility = Visibility.Collapsed;
        BookmarksPanel.Visibility = Visibility.Collapsed;
        FindBar.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ToggleFind_Click(object sender, RoutedEventArgs e)
    {
        ChaptersPanel.Visibility = Visibility.Collapsed;
        BookmarksPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        FindBar.Visibility = FindBar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (FindBar.Visibility == Visibility.Visible) FindTextBox.Focus();
    }

    private void CloseFind_Click(object sender, RoutedEventArgs e) => FindBar.Visibility = Visibility.Collapsed;

    private async void FindTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await FindNextAsync();
    }

    private async void FindNext_Click(object sender, RoutedEventArgs e) => await FindNextAsync();

    private async Task FindNextAsync()
    {
        var target = FindTextBox.Text.Trim();
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(_currentText)) return;

        var startSearchFrom = _readerChunkStart + 1;
        var index = _currentText.IndexOf(target, startSearchFrom, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
        {
            index = _currentText.IndexOf(target, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (index != -1)
        {
            _findIndex = index;
            await NavigateToCharacterAsync(index);
        }
        else
        {
            MessageBox.Show(this, $"未找到包含“{target}”的相关文本", "查找结果", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded || FontSizeValue == null) return;
        FontSizeValue.Text = $"{(int)e.NewValue} px";
        ReaderTextBlock.FontSize = e.NewValue;
        ReaderTextBlockSecond.FontSize = e.NewValue;
        ReaderTextBlock.LineHeight = e.NewValue * LineHeightSlider.Value;
        ReaderTextBlockSecond.LineHeight = e.NewValue * LineHeightSlider.Value;
        _library.Settings.FontSize = e.NewValue;
        ScheduleSettingsSave();

        if (_currentText.Length > 0 && _readingMode == "paged")
        {
            var offset = GetCurrentCharacterOffset();
            await ShowReaderPageAsync(offset, true);
        }
    }

    private async void LineHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded || LineHeightValue == null) return;
        LineHeightValue.Text = $"{e.NewValue:F2} x";
        ReaderTextBlock.LineHeight = ReaderTextBlock.FontSize * e.NewValue;
        ReaderTextBlockSecond.LineHeight = ReaderTextBlock.FontSize * e.NewValue;
        _library.Settings.LineHeightRatio = e.NewValue;
        ScheduleSettingsSave();

        if (_currentText.Length > 0 && _readingMode == "paged")
        {
            var offset = GetCurrentCharacterOffset();
            await ShowReaderPageAsync(offset, true);
        }
    }

    private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontFamilyComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string family) return;
        ReaderTextBlock.FontFamily = new FontFamily(family);
        ReaderTextBlockSecond.FontFamily = new FontFamily(family);
        if (_isLoaded)
        {
            _library.Settings.FontFamily = family;
            ScheduleSettingsSave();
        }
    }

    private void ReadingTheme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string theme }) { ApplyReadingTheme(theme, true); SettingsPanel.Visibility = Visibility.Collapsed; }
    }

    private void ApplyReadingTheme(string theme, bool save)
    {
        var colors = theme switch
        {
            "dark" => (Surface: "#20201E", Bar: "#292927", Text: "#E5E1D9", Secondary: "#B7B0A7"),
            "light" => (Surface: "#FFFFFF", Bar: "#F7F7F7", Text: "#202020", Secondary: "#686868"),
            "green" => (Surface: "#EAEFDE", Bar: "#E2E8D4", Text: "#23331C", Secondary: "#5E6C54"),
            _ => (Surface: "#FFFDF8", Bar: "#FAF7F0", Text: "#2B2926", Secondary: "#7A746C")
        };
        ReaderSurface.Background = BrushFrom(colors.Surface);
        ReaderView.Background = BrushFrom(colors.Surface);
        ReaderTextBlock.Foreground = BrushFrom(colors.Text);
        ReaderTextBlockSecond.Foreground = BrushFrom(colors.Text);
        ColumnDivider.Background = BrushFrom(theme == "dark" ? "#30FFFFFF" : "#18000000");
        PageTurnSheet.Background = BrushFrom(colors.Surface);
        ReaderTopBar.Background = BrushFrom(colors.Bar);
        ReaderStatusBar.Background = BrushFrom(colors.Bar);
        ChaptersPanel.Background = BrushFrom(colors.Bar);
        BookmarksPanel.Background = BrushFrom(colors.Bar);
        TtsBar.Background = BrushFrom(colors.Bar);
        ChaptersList.Foreground = BrushFrom(colors.Text);
        ChapterSummary.Foreground = BrushFrom(colors.Secondary);
        ReaderTitle.Foreground = BrushFrom(colors.Text);
        ReaderStatus.Foreground = BrushFrom(colors.Secondary);
        ReaderPageStatus.Foreground = BrushFrom(colors.Secondary);

        foreach (var button in new[] { PaperThemeButton, LightThemeButton, GreenThemeButton, DarkThemeButton })
        {
            var selected = string.Equals(button.Tag as string, theme, StringComparison.Ordinal);
            button.BorderBrush = selected ? (Brush)FindResource("AccentBrush") : BrushFrom("#26000000");
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
        }
        if (save && _isLoaded)
        {
            _library.Settings.ReadingTheme = theme;
            ScheduleSettingsSave();
        }
    }

    private async void ReadingColumns_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag } || !int.TryParse(tag, out var columns)) return;
        var characterOffset = GetCurrentCharacterOffset();
        ApplyReadingColumns(columns, true);
        SettingsPanel.Visibility = Visibility.Collapsed;
        if (_currentText.Length == 0) return;
        await ShowReaderPageAsync(characterOffset, true);
        UpdateReaderStatus();
    }

    private void PageTurnEffect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string effect }) { ApplyPageTurnEffect(effect, true); SettingsPanel.Visibility = Visibility.Collapsed; }
    }

    private void ApplyPageTurnEffect(string effect, bool save)
    {
        _pageTurnEffect = effect == "realistic" ? "realistic" : "smooth";
        foreach (var button in new[] { SmoothTurnButton, RealisticTurnButton })
        {
            var selected = string.Equals(button.Tag as string, _pageTurnEffect, StringComparison.Ordinal);
            button.BorderBrush = selected ? (Brush)FindResource("AccentBrush") : BrushFrom("#26000000");
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
        }
        if (save && _isLoaded)
        {
            _library.Settings.PageTurnEffect = _pageTurnEffect;
            ScheduleSettingsSave();
        }
    }

    private void ReadingMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string mode }) { ApplyReadingMode(mode, true); SettingsPanel.Visibility = Visibility.Collapsed; }
    }

    private void ApplyReadingMode(string mode, bool save)
    {
        _readingMode = mode == "scroll" ? "scroll" : "paged";
        var isScroll = _readingMode == "scroll";
        ReaderScrollViewer.VerticalScrollBarVisibility = isScroll ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        PreviousChunkButton.Visibility = isScroll ? Visibility.Collapsed : Visibility.Visible;
        NextChunkButton.Visibility = isScroll ? Visibility.Collapsed : Visibility.Visible;
        ReaderPageStatus.Visibility = isScroll ? Visibility.Collapsed : Visibility.Visible;
        foreach (var button in new[] { ScrollModeButton, PagedModeButton })
        {
            var selected = string.Equals(button.Tag as string, _readingMode, StringComparison.Ordinal);
            button.BorderBrush = selected ? (Brush)FindResource("AccentBrush") : BrushFrom("#26000000");
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
        }

        if (_currentText.Length > 0)
        {
            var offset = GetCurrentCharacterOffset();
            if (isScroll)
            {
                _ = ShowReaderChunkAsync(offset);
            }
            else
            {
                _ = ShowReaderPageAsync(offset, true);
            }
        }
        if (save && _isLoaded)
        {
            _library.Settings.ReadingMode = _readingMode;
            ScheduleSettingsSave();
        }
    }

    private void ApplyReadingColumns(int columns, bool save)
    {
        _readingColumns = columns == 1 ? 1 : 2;
        var doubleColumns = _readingColumns == 2;
        ReaderTextBlockSecond.Visibility = doubleColumns ? Visibility.Visible : Visibility.Collapsed;
        ColumnDivider.Visibility = doubleColumns ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumnSpan(ReaderTextBlock, doubleColumns ? 1 : 3);
        ReaderPageContainer.MaxWidth = doubleColumns ? 1320 : 900;

        foreach (var button in new[] { SingleColumnButton, DoubleColumnButton })
        {
            var selected = string.Equals(button.Tag as string, _readingColumns.ToString(), StringComparison.Ordinal);
            button.BorderBrush = selected ? (Brush)FindResource("AccentBrush") : BrushFrom("#26000000");
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
        }
        if (save && _isLoaded)
        {
            _library.Settings.ReadingColumns = _readingColumns;
            ScheduleSettingsSave();
        }
    }

    private void ApplySavedSettings()
    {
        var settings = _library.Settings;
        Width = Math.Max(MinWidth, settings.WindowWidth);
        Height = Math.Max(MinHeight, settings.WindowHeight);
        if (settings.IsWindowMaximized) WindowState = WindowState.Maximized;
        FontSizeSlider.Value = Math.Clamp(settings.FontSize, FontSizeSlider.Minimum, FontSizeSlider.Maximum);
        LineHeightSlider.Value = Math.Clamp(settings.LineHeightRatio, LineHeightSlider.Minimum, LineHeightSlider.Maximum);
        var fontItem = FontFamilyComboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, settings.FontFamily, StringComparison.OrdinalIgnoreCase));
        FontFamilyComboBox.SelectedItem = fontItem ?? FontFamilyComboBox.Items[0];
        ApplyReadingTheme(settings.ReadingTheme, false);
        ApplyReadingColumns(settings.ReadingColumns, false);
        ApplyPageTurnEffect(settings.PageTurnEffect, false);
        ApplyReadingMode(settings.ReadingMode ?? "paged", false);
    }

    private void ScheduleSettingsSave()
    {
        _settingsTimer.Stop();
        _settingsTimer.Start();
    }

    private async void SettingsTimer_Tick(object? sender, EventArgs e)
    {
        _settingsTimer.Stop();
        await _library.SaveSettingsAsync();
    }

    private static SolidColorBrush BrushFrom(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        return new SolidColorBrush(color);
    }
}
