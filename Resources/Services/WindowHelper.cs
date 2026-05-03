using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CassieWordCheck;

/// <summary>
/// Win32 API 封装——给窗口加上暗色标题栏和 Mica 毛玻璃效果喵~
/// 每次调用 DWM API 设置 Windows 11 的沉浸式暗色模式和背景模糊喵！
/// </summary>
public static class WindowHelper
{
    // 调用 dwmapi.dll 设置窗口属性喵~
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // DWM 常量：窗口属性 ID 喵~
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // 沉浸式暗色模式
    private const int DWMWA_MICA_EFFECT = 1029; // Mica 背景材质

    /// <summary>
    /// 启用暗色标题栏 + Mica 毛玻璃效果喵！
    /// 如果窗口还没加载，就等 Loaded 事件再调用喵~
    /// </summary>
    public static void EnableDarkTitleBar(this Window window)
    {
        if (window.IsLoaded)
            ApplyDarkMode(window);
        else
            window.Loaded += (_, _) => ApplyDarkMode(window);
    }

    // 实际调用 DWM 设置喵~
    private static void ApplyDarkMode(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        // 开启暗色标题栏喵！
        int useDark = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

        // 开启 Mica 毛玻璃效果喵！
        int useMica = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref useMica, sizeof(int));
    }
}
