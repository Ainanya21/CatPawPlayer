using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LeafReader.Helpers;

public enum BackdropType
{
    Auto = 0,
    None = 1,
    Mica = 2,
    Acrylic = 3,
    MicaAlt = 4
}

public static class WindowBackdropHelper
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public static bool ApplyBackdrop(Window window, BackdropType backdropType = BackdropType.Mica)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        int type = (int)backdropType;
        int result = DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref type, sizeof(int));
        return result == 0;
    }
}
