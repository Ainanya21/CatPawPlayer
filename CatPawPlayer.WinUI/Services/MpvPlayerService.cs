using System.Diagnostics;
using System.IO;

namespace CatPawPlayer.WinUI.Services;

public static class MpvPlayerService
{
    public const string DefaultMpvPath = "";

    public static string GetMpvExecutablePath()
    {
        try
        {
            var settings = App.Settings.LoadSettings();
            if (!string.IsNullOrWhiteSpace(settings.MpvPath) && File.Exists(settings.MpvPath))
                return settings.MpvPath;
        }
        catch { }

        return "";
    }

    public static bool IsMpvAvailable => !string.IsNullOrEmpty(GetMpvExecutablePath());

    public static bool PlayWithMpv(string mediaUrl, string title = "", Dictionary<string, string>? headers = null)
    {
        string exe = GetMpvExecutablePath();
        if (string.IsNullOrEmpty(exe)) return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
                UseShellExecute = false,
                CreateNoWindow = false,
            };

            var args = new List<string>();
            if (!string.IsNullOrEmpty(title))
            {
                args.Add($"--force-media-title=\"{title.Replace("\"", "\\\"")}\"");
            }
            args.Add("--user-agent=\"okhttp/4.9.0\"");

            if (headers != null && headers.Count > 0)
            {
                var headerList = new List<string>();
                foreach (var kv in headers)
                {
                    if (kv.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Add($"--user-agent=\"{kv.Value.Replace("\"", "\\\"")}\"");
                    }
                    else if (kv.Key.Equals("Referer", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Add($"--referrer=\"{kv.Value.Replace("\"", "\\\"")}\"");
                    }
                    else
                    {
                        headerList.Add($"{kv.Key}: {kv.Value}");
                    }
                }
                if (headerList.Count > 0)
                {
                    var headerStr = string.Join(",", headerList);
                    args.Add($"--http-header-fields=\"{headerStr.Replace("\"", "\\\"")}\"");
                }
            }

            args.Add($"\"{mediaUrl}\"");

            psi.Arguments = string.Join(" ", args);
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MpvPlayerService.PlayWithMpv] Error: {ex.Message}");
            return false;
        }
    }
}
