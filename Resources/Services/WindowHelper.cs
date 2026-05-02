using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CassieWordCheck;

public static class WindowHelper
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_MICA_EFFECT = 1029;

    public static void EnableDarkTitleBar(this Window window)
    {
        if (window.IsLoaded)
            ApplyDarkMode(window);
        else
            window.Loaded += (_, _) => ApplyDarkMode(window);
    }

    private static void ApplyDarkMode(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int useDark = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

        int useMica = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref useMica, sizeof(int));
    }
}
