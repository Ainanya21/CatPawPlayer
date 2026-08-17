using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace CatPawPlayer.WinUI;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "startup.log");
        try
        {
            File.WriteAllText(logPath, $"[START] Starting CatPawPlayer at {DateTime.Now}\n");

            WinRT.ComWrappersSupport.InitializeComWrappers();
            File.AppendAllText(logPath, "[OK] InitializeComWrappers\n");

            Application.Start(p =>
            {
                try
                {
                    File.AppendAllText(logPath, "[OK] Application.Start callback entered\n");
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    new App();
                    File.AppendAllText(logPath, "[OK] App instantiated\n");
                }
                catch (Exception inner)
                {
                    File.AppendAllText(logPath, $"[FATAL INNER] {inner}\n");
                }
            });
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"[FATAL MAIN] {ex}\n");
            Debug.WriteLine(ex);
        }
    }
}
