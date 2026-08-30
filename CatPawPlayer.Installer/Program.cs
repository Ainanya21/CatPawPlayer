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
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args != null && args.Any(a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("/u", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("-u", StringComparison.OrdinalIgnoreCase)))
        {
            bool isSilent = args.Any(a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("/s", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("-s", StringComparison.OrdinalIgnoreCase));
            RunUninstaller(isSilent);
            return;
        }

        Application.Run(new InstallerForm());
    }

    public static string SanitizeInstallPath(string? rawPath)
    {
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "CatPawPlayer");

        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return defaultPath;
        }

        try
        {
            rawPath = rawPath.Trim().TrimEnd('\\', '/');
            if (string.IsNullOrWhiteSpace(rawPath)) return defaultPath;

            // Handle disk drive letter without slash (e.g. "D:" -> "D:\")
            if (rawPath.Length == 2 && char.IsLetter(rawPath[0]) && rawPath[1] == ':')
            {
                rawPath += "\\";
            }

            string fullPath = Path.GetFullPath(rawPath);
            string root = Path.GetPathRoot(fullPath)?.TrimEnd('\\', '/') ?? "";

            // 1. If user selected a Drive Root (e.g. "D:\", "C:\", "E:\")
            if (string.Equals(fullPath.TrimEnd('\\', '/'), root, StringComparison.OrdinalIgnoreCase) || fullPath.Length <= 3)
            {
                return Path.Combine(fullPath.EndsWith("\\") ? fullPath : fullPath + "\\", "CatPawPlayer");
            }

            // 2. If the leaf folder name is already "CatPawPlayer"
            string leafName = Path.GetFileName(fullPath);
            if (string.Equals(leafName, "CatPawPlayer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(leafName, "CatPawPlayer.WinUI", StringComparison.OrdinalIgnoreCase))
            {
                return fullPath;
            }

            // 3. If user selected a generic parent directory (e.g. "D:\Software" or "D:\Program Files")
            return Path.Combine(fullPath, "CatPawPlayer");
        }
        catch
        {
            return defaultPath;
        }
    }

    private static void RunUninstaller(bool isSilent)
    {
        string targetDir = "";

        // 1. Try to get InstallLocation from Registry
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CatPawPlayer");
            if (key != null)
            {
                targetDir = key.GetValue("InstallLocation") as string ?? "";
            }
        }
        catch { }

        // 2. Fallback to current directory of the uninstaller
        if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
        {
            targetDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
        }

        if (!isSilent)
        {
            var result = MessageBox.Show(
                $"确定要完全卸载 猫爪播放器 (CatPawPlayer) 吗？\n\n卸载目录：{targetDir}",
                "猫爪播放器 卸载向导",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;
        }

        try
        {
            // 3. Terminate running processes
            foreach (var p in Process.GetProcessesByName("CatPawPlayer"))
            {
                try { p.Kill(); p.WaitForExit(3000); } catch { }
            }
            foreach (var p in Process.GetProcessesByName("node"))
            {
                try
                {
                    string? path = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path) && (path.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase) || path.Contains("CatPawPlayer", StringComparison.OrdinalIgnoreCase)))
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                }
                catch { }
            }

            // 4. Remove Shortcuts
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var ainanLnk = Path.Combine(desktopPath, "AinanPlayer.lnk");
                var catpawLnk = Path.Combine(desktopPath, "猫爪播放器.lnk");
                if (File.Exists(ainanLnk)) File.Delete(ainanLnk);
                if (File.Exists(catpawLnk)) File.Delete(catpawLnk);
            }
            catch { }

            try
            {
                var programsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                var startDirAinan = Path.Combine(programsPath, "AinanPlayer");
                var startDirCatpaw = Path.Combine(programsPath, "猫爪播放器");
                if (Directory.Exists(startDirAinan)) Directory.Delete(startDirAinan, true);
                if (Directory.Exists(startDirCatpaw)) Directory.Delete(startDirCatpaw, true);

                var singleAinanLnk = Path.Combine(programsPath, "AinanPlayer.lnk");
                var singleCatpawLnk = Path.Combine(programsPath, "猫爪播放器.lnk");
                if (File.Exists(singleAinanLnk)) File.Delete(singleAinanLnk);
                if (File.Exists(singleCatpawLnk)) File.Delete(singleCatpawLnk);
            }
            catch { }

            // 5. Remove Registry Keys
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CatPawPlayer", false);
            }
            catch { }

            // 6. Safe File Deletion Guard
            bool isDriveRootOrSystemFolder = false;
            try
            {
                string full = Path.GetFullPath(targetDir).TrimEnd('\\', '/');
                string root = Path.GetPathRoot(full)?.TrimEnd('\\', '/') ?? "";

                if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase) || full.Length <= 3)
                {
                    isDriveRootOrSystemFolder = true;
                }

                var dirInfo = new DirectoryInfo(targetDir);
                if (dirInfo.Parent == null) isDriveRootOrSystemFolder = true;

                string[] protectedFolders = [
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                ];

                foreach (var pf in protectedFolders)
                {
                    if (!string.IsNullOrEmpty(pf) && string.Equals(Path.GetFullPath(targetDir).TrimEnd('\\', '/'), Path.GetFullPath(pf).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    {
                        isDriveRootOrSystemFolder = true;
                        break;
                    }
                }
            }
            catch { }

            if (isDriveRootOrSystemFolder)
            {
                // CRITICAL SAFETY SHIELD: ONLY delete CatPawPlayer specific files, NEVER delete the directory!
                string[] appFiles = [
                    "CatPawPlayer.exe", "CatPawPlayer.dll", "CatPawPlayer.exp", "CatPawPlayer.lib", "CatPawPlayer.pdb",
                    "CatPawPlayer.deps.json", "CatPawPlayer.runtimeconfig.json", "spiderHost.js", "cat_spider_server.js",
                    "douer_spider_server.js", "Uninstall.exe", "uninstall.cmd", "app.manifest", "D3DCompiler_47.dll",
                    "WinRT.Runtime.dll", "resources.pri"
                ];
                string[] appDirs = [
                    "node", "assets", "controls", "pages", "microsoft.ui.xaml"
                ];

                foreach (var f in appFiles)
                {
                    try { var p = Path.Combine(targetDir, f); if (File.Exists(p)) File.Delete(p); } catch { }
                }
                foreach (var d in appDirs)
                {
                    try { var p = Path.Combine(targetDir, d); if (Directory.Exists(p)) Directory.Delete(p, true); } catch { }
                }
            }
            else if (Directory.Exists(targetDir))
            {
                // Dedicated folder (e.g. D:\CatPawPlayer)
                try
                {
                    foreach (var file in Directory.GetFiles(targetDir))
                    {
                        if (Path.GetFileName(file).Equals("Uninstall.exe", StringComparison.OrdinalIgnoreCase)) continue;
                        try { File.Delete(file); } catch { }
                    }
                    foreach (var dir in Directory.GetDirectories(targetDir))
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                }
                catch { }

                // Delayed self-delete for Uninstall.exe and the dedicated empty directory
                string tempBat = Path.Combine(Path.GetTempPath(), $"catpaw_uninst_{Guid.NewGuid():N}.bat");
                string batContent = $@"@echo off
ping 127.0.0.1 -n 2 > nul
del /f /q ""{Path.Combine(targetDir, "Uninstall.exe")}"" > nul 2>&1
rd /s /q ""{targetDir}"" > nul 2>&1
del ""%~f0""
";
                File.WriteAllText(tempBat, batContent);
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{tempBat}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }

            if (!isSilent)
            {
                MessageBox.Show("猫爪播放器 (CatPawPlayer) 已成功从您的电脑中卸载！", "卸载完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            if (!isSilent)
            {
                MessageBox.Show($"卸载过程中发生错误: {ex.Message}", "卸载提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
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
        Text = "AinanPlayer (猫爪播放器) - 安装向导 v2.1.3";
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

        _defaultPath = Program.SanitizeInstallPath(DetectExistingInstallPath(out bool isUpgrade));

        if (isUpgrade)
        {
            Text = "AinanPlayer (猫爪播放器) - 覆盖升级向导 v2.1.3";
        }
        else
        {
            Text = "AinanPlayer (猫爪播放器) - 安装向导 v2.1.3";
        }

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
            Text = isUpgrade ? "猫爪播放器 - 覆盖升级" : "猫爪播放器 (CatPawPlayer)",
            Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(92, 18),
            AutoSize = true
        };

        var versionLabel = new Label
        {
            Text = isUpgrade
                ? "版本 v2.0.1 正式版 · 已自动追踪到已安装目录 (保持配置无缝升级)"
                : "版本 v2.0.1 正式版 · 原生极速影视流媒体客户端",
            Font = new Font("Microsoft YaHei UI", 9F),
            ForeColor = isUpgrade ? Color.FromArgb(52, 211, 153) : Color.FromArgb(161, 161, 170),
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
        _pathBox.Leave += (s, e) =>
        {
            _pathBox.Text = Program.SanitizeInstallPath(_pathBox.Text);
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
                _pathBox.Text = Program.SanitizeInstallPath(dialog.SelectedPath);
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
            Text = isUpgrade ? "立即覆盖升级" : "立即安装",
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

        var targetDir = Program.SanitizeInstallPath(_pathBox.Text.Trim());
        _pathBox.Text = targetDir;

        _mainContentPanel.Visible = false;
        _progressContentPanel.Visible = true;
        _installBtn.Enabled = false;
        _cancelBtn.Enabled = false;

        await Task.Run(() =>
        {
            try
            {
                // 1. Terminate running instance if any
                UpdateStatus("正在关闭正在运行的猫爪播放器与后台服务进程...", 10);
                foreach (var p in Process.GetProcessesByName("CatPawPlayer"))
                {
                    try { p.Kill(); p.WaitForExit(3000); } catch { }
                }
                foreach (var p in Process.GetProcessesByName("node"))
                {
                    try
                    {
                        string? path = p.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(path) && (path.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase) || path.Contains("CatPawPlayer", StringComparison.OrdinalIgnoreCase)))
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                        }
                    }
                    catch { }
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

                // Always write latest AppIcon.ico to ensure icons update on override installs
                try
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
                catch { }

                // Copy running installer as Uninstall.exe
                var currentInstallerExe = Process.GetCurrentProcess().MainModule?.FileName;
                var uninstallerPath = Path.Combine(targetDir, "Uninstall.exe");
                if (!string.IsNullOrEmpty(currentInstallerExe) && File.Exists(currentInstallerExe))
                {
                    try { File.Copy(currentInstallerExe, uninstallerPath, true); } catch { }
                }

                // 4. Create Shortcuts
                UpdateStatus("正在创建桌面与系统快捷方式...", 90);
                if (_desktopShortcutCheck.Checked)
                {
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var oldDesktopLnk = Path.Combine(desktopPath, "猫爪播放器.lnk");
                    if (File.Exists(oldDesktopLnk)) try { File.Delete(oldDesktopLnk); } catch { }

                    CreateShortcut(Path.Combine(desktopPath, "AinanPlayer.lnk"), mainExe, targetDir, iconPath);
                }

                if (_startMenuShortcutCheck.Checked)
                {
                    var programsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    var oldStartDir = Path.Combine(programsPath, "猫爪播放器");
                    if (Directory.Exists(oldStartDir)) try { Directory.Delete(oldStartDir, true); } catch { }

                    var startMenuDir = Path.Combine(programsPath, "AinanPlayer");
                    Directory.CreateDirectory(startMenuDir);
                    CreateShortcut(Path.Combine(startMenuDir, "AinanPlayer.lnk"), mainExe, targetDir, iconPath);
                    if (File.Exists(uninstallerPath))
                    {
                        CreateShortcut(Path.Combine(startMenuDir, "卸载AinanPlayer.lnk"), uninstallerPath, targetDir, iconPath, "--uninstall");
                    }
                }

                // Flush Windows Explorer Shell Icon Cache
                RefreshShellIcons();

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

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconPath, string arguments = "")
    {
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type == null) return;
            dynamic shell = Activator.CreateInstance(type)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Arguments = arguments;
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

    private static string DetectExistingInstallPath(out bool isUpgrade)
    {
        isUpgrade = false;

        // 1. Check HKCU Uninstall Registry
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CatPawPlayer");
            if (key != null)
            {
                var loc = key.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc) &&
                    (File.Exists(Path.Combine(loc, "CatPawPlayer.exe")) || File.Exists(Path.Combine(loc, "CatPawPlayer.dll"))))
                {
                    isUpgrade = true;
                    return loc;
                }
            }
        }
        catch { }

        // 2. Check HKLM Uninstall Registry
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CatPawPlayer");
            if (key != null)
            {
                var loc = key.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc) &&
                    (File.Exists(Path.Combine(loc, "CatPawPlayer.exe")) || File.Exists(Path.Combine(loc, "CatPawPlayer.dll"))))
                {
                    isUpgrade = true;
                    return loc;
                }
            }
        }
        catch { }

        // 3. Check Desktop Shortcut Target
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string[] desktopLnks = [Path.Combine(desktopPath, "AinanPlayer.lnk"), Path.Combine(desktopPath, "猫爪播放器.lnk")];
            foreach (var desktopLnk in desktopLnks)
            {
                if (File.Exists(desktopLnk))
                {
                    var type = Type.GetTypeFromProgID("WScript.Shell");
                    if (type != null)
                    {
                        dynamic shell = Activator.CreateInstance(type)!;
                        dynamic shortcut = shell.CreateShortcut(desktopLnk);
                        string target = shortcut.TargetPath;
                        if (!string.IsNullOrEmpty(target) && File.Exists(target))
                        {
                            var dir = Path.GetDirectoryName(target);
                            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                            {
                                isUpgrade = true;
                                return dir;
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // 4. Check Start Menu Shortcut Target
        try
        {
            var programsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string[] startLnks = [
                Path.Combine(programsPath, "AinanPlayer", "AinanPlayer.lnk"),
                Path.Combine(programsPath, "猫爪播放器", "猫爪播放器.lnk")
            ];
            foreach (var startMenuLnk in startLnks)
            {
                if (File.Exists(startMenuLnk))
                {
                    var type = Type.GetTypeFromProgID("WScript.Shell");
                    if (type != null)
                    {
                        dynamic shell = Activator.CreateInstance(type)!;
                        dynamic shortcut = shell.CreateShortcut(startMenuLnk);
                        string target = shortcut.TargetPath;
                        if (!string.IsNullOrEmpty(target) && File.Exists(target))
                        {
                            var dir = Path.GetDirectoryName(target);
                            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                            {
                                isUpgrade = true;
                                return dir;
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // 5. Default Fallback Path (%LocalAppData%\Programs\CatPawPlayer)
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "CatPawPlayer");

        if (Directory.Exists(defaultPath) &&
            (File.Exists(Path.Combine(defaultPath, "CatPawPlayer.exe")) || File.Exists(Path.Combine(defaultPath, "CatPawPlayer.dll"))))
        {
            isUpgrade = true;
        }

        return defaultPath;
    }

    private static void RegisterUninstall(string installDir, string mainExe, string iconPath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\CatPawPlayer");
            if (key != null)
            {
                var uninstallerExe = Path.Combine(installDir, "Uninstall.exe");
                string uninstallString = File.Exists(uninstallerExe)
                    ? $"\"{uninstallerExe}\" --uninstall"
                    : $"\"{mainExe}\" --uninstall";

                key.SetValue("DisplayName", "AinanPlayer (猫爪播放器)");
                key.SetValue("DisplayVersion", Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.1.1");
                key.SetValue("Publisher", "CatPaw Studio");
                key.SetValue("InstallLocation", installDir);
                key.SetValue("DisplayIcon", File.Exists(iconPath) ? iconPath : mainExe);
                key.SetValue("UninstallString", uninstallString);
                key.SetValue("QuietUninstallString", $"\"{uninstallerExe}\" --uninstall --silent");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public static void RefreshShellIcons()
    {
        try
        {
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
        catch { }
    }
}
