using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CatPawPlayer.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }
}

public class InstallerForm : Form
{
    private readonly TextBox _pathBox;
    private readonly CheckBox _desktopShortcutCheck;
    private readonly CheckBox _startMenuShortcutCheck;
    private readonly CheckBox _launchAfterCheck;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Button _installBtn;
    private readonly Button _cancelBtn;

    private readonly Panel _mainContentPanel;
    private readonly Panel _progressContentPanel;
    private readonly Panel _completeContentPanel;

    private readonly string _defaultPath;

    public InstallerForm()
    {
        Text = "猫爪播放器 (CatPawPlayer) - 安装向导 v1.0.2";
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.White;
        Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);

        // Explicit Form Dimensions (604 x 441 Client Area)
        ClientSize = new Size(604, 441);
        Size = new Size(620, 480);
        MinimumSize = new Size(620, 480);

        // Load App Icon
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var iconStream = asm.GetManifestResourceStream("AppIcon.ico")
                ?? asm.GetManifestResourceStream("CatPawPlayer.Installer.AppIcon.ico");
            if (iconStream != null)
                Icon = new Icon(iconStream);
        }
        catch { }

        _defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "CatPawPlayer");

        // ═══════════════════════════════════════════════════════════════
        // 1. TOP HEADER BANNER (Fixed Area: 0, 0, 604, 96)
        // ═══════════════════════════════════════════════════════════════
        var headerPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(604, 96),
            BackColor = Color.FromArgb(20, 20, 24)
        };

        var logoBox = new PictureBox
        {
            Location = new Point(20, 18),
            Size = new Size(60, 60),
            SizeMode = PictureBoxSizeMode.Zoom
        };

        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var pngStream = asm.GetManifestResourceStream("AppIcon.png")
                ?? asm.GetManifestResourceStream("CatPawPlayer.Installer.AppIcon.png");
            if (pngStream != null)
                logoBox.Image = Image.FromStream(pngStream);
        }
        catch { }

        var titleLabel = new Label
        {
            Text = "猫爪播放器 (CatPawPlayer)",
            Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(92, 18),
            AutoSize = true
        };

        var versionLabel = new Label
        {
            Text = "版本 v1.0.2.0 正式版 · 原生极速影视流媒体客户端",
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = Color.FromArgb(161, 161, 170),
            Location = new Point(92, 54),
            AutoSize = true
        };

        headerPanel.Controls.Add(logoBox);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(versionLabel);
        Controls.Add(headerPanel);

        // ═══════════════════════════════════════════════════════════════
        // 2. MAIN CENTER CONTENT AREA (Fixed Area: 0, 96, 604, 280)
        // ═══════════════════════════════════════════════════════════════
        _mainContentPanel = new Panel
        {
            Location = new Point(0, 96),
            Size = new Size(604, 280),
            BackColor = Color.White
        };

        // 2.1 Top: Installation Options
        var optionsLabel = new Label
        {
            Text = "安装选项：",
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 39, 42),
            Location = new Point(28, 16),
            AutoSize = true
        };

        _desktopShortcutCheck = new CheckBox
        {
            Text = "创建桌面快捷方式",
            Checked = true,
            Location = new Point(32, 44),
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9.5F)
        };

        _startMenuShortcutCheck = new CheckBox
        {
            Text = "创建「开始」菜单快捷方式",
            Checked = true,
            Location = new Point(32, 72),
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9.5F)
        };

        _launchAfterCheck = new CheckBox
        {
            Text = "安装完成后立即启动猫爪播放器",
            Checked = true,
            Location = new Point(32, 100),
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9.5F)
        };

        // 2.2 Middle-Lower: Installation Path
        var pathLabel = new Label
        {
            Text = "安装路径 (支持直接覆盖旧版本升级)：",
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 39, 42),
            Location = new Point(28, 142),
            AutoSize = true
        };

        _pathBox = new TextBox
        {
            Text = _defaultPath,
            Location = new Point(30, 170),
            Size = new Size(440, 28),
            Font = new Font("Microsoft YaHei UI", 9.5F)
        };

        var browseBtn = new Button
        {
            Text = "浏览...",
            Location = new Point(478, 168),
            Size = new Size(88, 30),
            FlatStyle = FlatStyle.System
        };
        browseBtn.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择猫爪播放器的安装目录：",
                SelectedPath = _pathBox.Text,
                UseDescriptionForTitle = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _pathBox.Text = dialog.SelectedPath;
            }
        };

        _mainContentPanel.Controls.Add(optionsLabel);
        _mainContentPanel.Controls.Add(_desktopShortcutCheck);
        _mainContentPanel.Controls.Add(_startMenuShortcutCheck);
        _mainContentPanel.Controls.Add(_launchAfterCheck);
        _mainContentPanel.Controls.Add(pathLabel);
        _mainContentPanel.Controls.Add(_pathBox);
        _mainContentPanel.Controls.Add(browseBtn);
        Controls.Add(_mainContentPanel);

        // 2.3 Progress View (0, 96, 604, 280)
        _progressContentPanel = new Panel
        {
            Location = new Point(0, 96),
            Size = new Size(604, 280),
            BackColor = Color.White,
            Visible = false
        };

        _statusLabel = new Label
        {
            Text = "准备安装中...",
            Location = new Point(30, 70),
            Size = new Size(540, 26),
            Font = new Font("Microsoft YaHei UI", 10F)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(30, 106),
            Size = new Size(536, 22),
            Style = ProgressBarStyle.Continuous
        };

        _progressContentPanel.Controls.Add(_statusLabel);
        _progressContentPanel.Controls.Add(_progressBar);
        Controls.Add(_progressContentPanel);

        // 2.4 Complete View (0, 96, 604, 280)
        _completeContentPanel = new Panel
        {
            Location = new Point(0, 96),
            Size = new Size(604, 280),
            BackColor = Color.White,
            Visible = false
        };

        var successTitle = new Label
        {
            Text = "🎉 猫爪播放器 已安装成功！",
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(5, 150, 105),
            Location = new Point(30, 45),
            AutoSize = true
        };

        var successDesc = new Label
        {
            Text = "您现在可以点击「完成」立即开启极速 4K 原生影视流媒体体验。\n后续若有新版本，直接运行安装包覆盖即可平滑升级，所有订阅与收藏均会自动保留。",
            Location = new Point(30, 88),
            Size = new Size(540, 65),
            Font = new Font("Microsoft YaHei UI", 9.5F),
            ForeColor = Color.FromArgb(82, 82, 91)
        };

        _completeContentPanel.Controls.Add(successTitle);
        _completeContentPanel.Controls.Add(successDesc);
        Controls.Add(_completeContentPanel);

        // ═══════════════════════════════════════════════════════════════
        // 3. BOTTOM ACTION BAR (Fixed Area: 0, 376, 604, 65)
        // ═══════════════════════════════════════════════════════════════
        var bottomPanel = new Panel
        {
            Location = new Point(0, 376),
            Size = new Size(604, 65),
            BackColor = Color.FromArgb(244, 244, 246)
        };

        _installBtn = new Button
        {
            Text = "立即安装",
            Location = new Point(372, 16),
            Size = new Size(110, 34),
            BackColor = Color.FromArgb(99, 102, 241),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
        };
        _installBtn.FlatAppearance.BorderSize = 0;
        _installBtn.Click += async (s, e) => await StartInstallationAsync();

        _cancelBtn = new Button
        {
            Text = "取消",
            Location = new Point(492, 16),
            Size = new Size(88, 34),
            FlatStyle = FlatStyle.System
        };
        _cancelBtn.Click += (s, e) => Close();

        bottomPanel.Controls.Add(_installBtn);
        bottomPanel.Controls.Add(_cancelBtn);
        Controls.Add(bottomPanel);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ClientSize = new Size(604, 441);
        CenterToScreen();
    }

    private async Task StartInstallationAsync()
    {
        if (_installBtn.Text == "完成")
        {
            if (_launchAfterCheck.Checked)
            {
                var exePath = Path.Combine(_pathBox.Text.Trim(), "CatPawPlayer.exe");
                if (File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = _pathBox.Text.Trim(), UseShellExecute = true });
                }
            }
            Close();
            return;
        }

        var targetDir = _pathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(targetDir))
        {
            MessageBox.Show("请选择有效的安装路径！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _mainContentPanel.Visible = false;
        _progressContentPanel.Visible = true;
        _installBtn.Enabled = false;
        _cancelBtn.Enabled = false;

        await Task.Run(() =>
        {
            try
            {
                // 1. Terminate running instance if any
                UpdateStatus("正在关闭正在运行的猫爪播放器进程...", 10);
                foreach (var p in Process.GetProcessesByName("CatPawPlayer"))
                {
                    try { p.Kill(); p.WaitForExit(3000); } catch { }
                }

                // 2. Prepare directory
                UpdateStatus("正在准备安装目录...", 20);
                Directory.CreateDirectory(targetDir);

                // 3. Extract payload.zip
                UpdateStatus("正在释放应用程序核心与资源组件...", 35);
                var asm = Assembly.GetExecutingAssembly();
                var resourceName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("payload.zip"));

                if (resourceName != null)
                {
                    using var stream = asm.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var archive = new ZipArchive(stream);
                        int total = archive.Entries.Count;
                        int current = 0;

                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
                            {
                                Directory.CreateDirectory(Path.Combine(targetDir, entry.FullName));
                                continue;
                            }

                            var destPath = Path.Combine(targetDir, entry.FullName);
                            var dirName = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(dirName)) Directory.CreateDirectory(dirName);

                            try
                            {
                                entry.ExtractToFile(destPath, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Extract error for {destPath}: {ex.Message}");
                            }

                            current++;
                            if (current % 15 == 0 || current == total)
                            {
                                int pct = 35 + (int)(50.0 * current / total);
                                UpdateStatus($"正在写入文件 ({current}/{total})...", pct);
                            }
                        }
                    }
                }

                var mainExe = Path.Combine(targetDir, "CatPawPlayer.exe");
                var iconPath = Path.Combine(targetDir, "Assets", "AppIcon.ico");

                // Ensure icon exists in target directory
                if (!File.Exists(iconPath))
                {
                    var assetsDir = Path.Combine(targetDir, "Assets");
                    Directory.CreateDirectory(assetsDir);
                    using var iconStream = asm.GetManifestResourceStream("AppIcon.ico")
                        ?? asm.GetManifestResourceStream("CatPawPlayer.Installer.AppIcon.ico");
                    if (iconStream != null)
                    {
                        using var fs = File.Create(iconPath);
                        iconStream.CopyTo(fs);
                    }
                }

                // 4. Create Shortcuts
                UpdateStatus("正在创建桌面与系统快捷方式...", 90);
                if (_desktopShortcutCheck.Checked)
                {
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    CreateShortcut(Path.Combine(desktopPath, "猫爪播放器.lnk"), mainExe, targetDir, iconPath);
                }

                if (_startMenuShortcutCheck.Checked)
                {
                    var startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "猫爪播放器");
                    Directory.CreateDirectory(startMenuDir);
                    CreateShortcut(Path.Combine(startMenuDir, "猫爪播放器.lnk"), mainExe, targetDir, iconPath);
                }

                // 5. Register in Windows Add/Remove Programs (Registry)
                UpdateStatus("正在注册系统应用信息...", 95);
                RegisterUninstall(targetDir, mainExe, iconPath);

                UpdateStatus("安装完成！", 100);
            }
            catch (Exception ex)
            {
                Invoke(() =>
                {
                    MessageBox.Show($"安装过程中发生错误: {ex.Message}", "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                });
            }
        });

        _progressContentPanel.Visible = false;
        _completeContentPanel.Visible = true;
        _installBtn.Text = "完成";
        _installBtn.Enabled = true;
        _cancelBtn.Visible = false;
    }

    private void UpdateStatus(string text, int progress)
    {
        Invoke(() =>
        {
            _statusLabel.Text = text;
            _progressBar.Value = Math.Min(100, Math.Max(0, progress));
        });
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconPath)
    {
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type == null) return;
            dynamic shell = Activator.CreateInstance(type)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = "猫爪播放器 (CatPawPlayer) - 极速原生影视流媒体客户端";
            if (File.Exists(iconPath))
            {
                shortcut.IconLocation = $"{iconPath},0";
            }
            else
            {
                shortcut.IconLocation = $"{targetPath},0";
            }
            shortcut.Save();
        }
        catch { }
    }

    private static void RegisterUninstall(string installDir, string mainExe, string iconPath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CatPawPlayer");
            if (key != null)
            {
                key.SetValue("DisplayName", "猫爪播放器 (CatPawPlayer)");
                key.SetValue("DisplayVersion", "1.0.0");
                key.SetValue("Publisher", "CatPaw Studio");
                key.SetValue("InstallLocation", installDir);
                key.SetValue("DisplayIcon", File.Exists(iconPath) ? iconPath : mainExe);
                key.SetValue("UninstallString", $"cmd /c rmdir /s /q \"{installDir}\"");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
