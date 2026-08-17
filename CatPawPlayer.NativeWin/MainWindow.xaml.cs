using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace CatPawPlayer.NativeWin
{
    public partial class MainWindow : Window
    {
        private Process? _nodeProcess;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMSBT_MAINWINDOW = 2; // Mica Material

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            StartCatPawNodeService();
            InitializeWebView();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int backdropType = DWMSBT_MAINWINDOW;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            }
            catch { }
        }

        private void StartCatPawNodeService()
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string distElectronPath = Path.Combine(baseDir, "proxyServer.js");

                if (!File.Exists(distElectronPath))
                {
                    distElectronPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "CatPawPlayer", "dist-electron", "proxyServer.js"));
                }
                if (!File.Exists(distElectronPath))
                {
                    distElectronPath = @"D:\yu896367449\Antigravity Chat\App develope\CatPawPlayer\dist-electron\proxyServer.js";
                }

                if (File.Exists(distElectronPath))
                {
                    string nodeExe = @"D:\Nodejs\node.exe";
                    if (!File.Exists(nodeExe)) nodeExe = "node";

                    var psi = new ProcessStartInfo
                    {
                        FileName = nodeExe,
                        Arguments = $"\"{distElectronPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    _nodeProcess = Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Node Microservice Start Error]: {ex.Message}");
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CatPawPlayer", "WebView2Data");
                Directory.CreateDirectory(userDataFolder);

                var options = new CoreWebView2EnvironmentOptions("--enable-gpu-rasterization --enable-zero-copy --ignore-gpu-blocklist");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                await MainWebView.EnsureCoreWebView2Async(env);

                MainWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                MainWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                string distFolder = Path.Combine(AppContext.BaseDirectory, "dist");
                if (!Directory.Exists(distFolder))
                {
                    distFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CatPawPlayer", "dist"));
                }
                if (!Directory.Exists(distFolder))
                {
                    distFolder = @"D:\yu896367449\Antigravity Chat\App develope\CatPawPlayer\dist";
                }

                if (Directory.Exists(distFolder))
                {
                    MainWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "app.catpaw.local",
                        distFolder,
                        CoreWebView2HostResourceAccessKind.Allow
                    );
                    MainWebView.CoreWebView2.Navigate("https://app.catpaw.local/index.html");
                }
                else
                {
                    MessageBox.Show($"未找到构建资源包目录: {distFolder}\n请确保 CatPawPlayer/dist 存在。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失败: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                if (_nodeProcess != null && !_nodeProcess.HasExited)
                {
                    _nodeProcess.Kill(true);
                }
            }
            catch { }
        }
    }
}
