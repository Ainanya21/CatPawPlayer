using CatPawPlayer.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;

namespace CatPawPlayer.WinUI.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loading = false;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        _loading = true;
        var settings = App.Settings.LoadSettings();
        QuarkCookieBox.Text = settings.QuarkCookie ?? string.Empty;
        BaiduCookieBox.Text = settings.BaiduCookie ?? string.Empty;
        Cookie115Box.Text = settings.Cookie115 ?? string.Empty;

        // MPV Settings
        UseMpvSwitch.IsOn = settings.UseExternalMpv;
        MpvPathBox.Text = settings.MpvPath ?? string.Empty;
        UpdateMpvStatus(MpvPathBox.Text);

        switch (settings.Theme)
        {
            case "Light":
                ThemeRadioButtons.SelectedIndex = 0;
                break;
            case "Dark":
                ThemeRadioButtons.SelectedIndex = 1;
                break;
            default:
                ThemeRadioButtons.SelectedIndex = 2;
                break;
        }
        _loading = false;
    }

    private void UpdateMpvStatus(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            MpvStatusText.Text = "⚪ 未配置 MPV 播放器 (将使用内置原生播放器)";
            MpvStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            TestMpvBtn.IsEnabled = false;
        }
        else if (File.Exists(path))
        {
            MpvStatusText.Text = "🟢 已检测到 MPV 播放器 (就绪)";
            MpvStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
            TestMpvBtn.IsEnabled = true;
        }
        else
        {
            MpvStatusText.Text = "🔴 未找到指定的 MPV 执行文件，请检查路径";
            MpvStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
            TestMpvBtn.IsEnabled = false;
        }
    }

    private void UseMpvSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var settings = App.Settings.LoadSettings();
        settings.UseExternalMpv = UseMpvSwitch.IsOn;
        App.Settings.SaveSettings(settings);
    }

    private void MpvPathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        var path = MpvPathBox.Text.Trim();
        var settings = App.Settings.LoadSettings();
        settings.MpvPath = path;
        App.Settings.SaveSettings(settings);
        UpdateMpvStatus(path);
    }

    private async void BrowseMpvBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add("*");

            if (MainWindow.Instance != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                MpvPathBox.Text = file.Path;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BrowseMpv] Error: {ex.Message}");
        }
    }

    private void TestMpvBtn_Click(object sender, RoutedEventArgs e)
    {
        MpvPlayerService.PlayWithMpv("https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8", "MPV 极速播放测试");
    }

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (ThemeRadioButtons.SelectedItem is RadioButton rb)
        {
            var theme = rb.Tag?.ToString() ?? "Default";
            var settings = App.Settings.LoadSettings();
            settings.Theme = theme;
            App.Settings.SaveSettings(settings);

            // Apply theme immediately
            MainWindow.Instance?.SetAppTheme(theme);
        }
    }

    private void AccentColorBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string colorHex)
        {
            var settings = App.Settings.LoadSettings();
            settings.AccentColor = colorHex;
            App.Settings.SaveSettings(settings);
        }
    }

    private void AccentColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_loading) return;
        var settings = App.Settings.LoadSettings();
        settings.AccentColor = args.NewColor.ToString();
        App.Settings.SaveSettings(settings);
    }

    private void OpenInAppConfigCenter_Click(object sender, RoutedEventArgs e)
    {
        var configSite = App.Sites.FirstOrDefault(s => s.Key == "baseset" || s.Name.Contains("配置"));
        MainWindow.Instance?.SelectCategory(configSite?.Key ?? "baseset");
    }

    private void SaveQuark_Click(object sender, RoutedEventArgs e)
    {
        var settings = App.Settings.LoadSettings();
        settings.QuarkCookie = QuarkCookieBox.Text.Trim();
        App.Settings.SaveSettings(settings);
    }

    private void SaveBaidu_Click(object sender, RoutedEventArgs e)
    {
        var settings = App.Settings.LoadSettings();
        settings.BaiduCookie = BaiduCookieBox.Text.Trim();
        App.Settings.SaveSettings(settings);
    }

    private void Save115_Click(object sender, RoutedEventArgs e)
    {
        var settings = App.Settings.LoadSettings();
        settings.Cookie115 = Cookie115Box.Text.Trim();
        App.Settings.SaveSettings(settings);
    }
}
