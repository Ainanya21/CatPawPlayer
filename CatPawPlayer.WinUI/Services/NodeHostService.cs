using System.Diagnostics;

namespace CatPawPlayer.WinUI.Services;

public class NodeHostService : IDisposable
{
    private Process? _process;

    private static readonly string[] PossibleNodePaths =
    [
        Path.Combine(AppContext.BaseDirectory, "node", "node.exe"),
        Path.Combine(AppContext.BaseDirectory, "node.exe"),
        @"D:\Nodejs\node.exe",
        "node"
    ];

    private static readonly string[] PossibleScriptPaths =
    [
        Path.Combine(AppContext.BaseDirectory, "spiderHost.js"),
        @"D:\yu896367449\Antigravity Chat\App develope\CatPawPlayer.WinUI\spiderHost.js",
        Path.Combine(AppContext.BaseDirectory, "proxyServer.js"),
    ];

    public void Start()
    {
        try
        {
            string? nodeExe = PossibleNodePaths.FirstOrDefault(p => p == "node" || File.Exists(p)) ?? "node";
            string? scriptPath = PossibleScriptPaths.FirstOrDefault(File.Exists);

            if (scriptPath == null)
            {
                Debug.WriteLine("[NodeHostService] spiderHost.js not found, skipping Node start.");
                return;
            }

            Debug.WriteLine($"[NodeHostService] Launching Node spider service using: {nodeExe} script: {scriptPath}");

            var psi = new ProcessStartInfo
            {
                FileName = nodeExe,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            _process = Process.Start(psi);
            Debug.WriteLine($"[NodeHostService] Node microservice started (PID: {_process?.Id})");

            // Give it a moment to boot
            Thread.Sleep(800);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NodeHostService] Start failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch { }
    }
}
