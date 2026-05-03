using System.Threading;
using System.Windows;

namespace CassieWordCheck;

/// <summary>
/// 应用入口——负责单实例检查和全局样式覆盖喵~
/// </summary>
public partial class App : Application
{
    // 全局 Mutex，确保只有一个实例在运行喵！
    private static Mutex? _mutex;
    private const string MutexId = "CassieWordCheck_SingleInstance";

    protected override void OnStartup(StartupEventArgs e)
    {
        // 尝试创建 Mutex，如果已存在说明有另一个实例在跑喵~
        _mutex = new Mutex(true, MutexId, out var createdNew);

        if (!createdNew)
        {
            MessageBox.Show("程序已在运行中。\nThe app is already running.",
                "CASSIE CWC Tool", MessageBoxButton.OK, MessageBoxImage.Information);
            _mutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // 覆盖 Window 默认样式元数据，让全局样式对 Window 生效喵~
        FrameworkElement.StyleProperty.OverrideMetadata(
            typeof(Window),
            new FrameworkPropertyMetadata(null));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 退出时释放 Mutex，避免下次启动报"已存在"喵~
        if (_mutex is not null)
        {
            _mutex.ReleaseMutex();
            _mutex.Close();
        }
        base.OnExit(e);
    }
}
