using System.Threading;
using System.Windows;

namespace CassieWordCheck;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexId = "CassieWordCheck_SingleInstance";

    protected override void OnStartup(StartupEventArgs e)
    {
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
        FrameworkElement.StyleProperty.OverrideMetadata(
            typeof(Window),
            new FrameworkPropertyMetadata(null));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex is not null)
        {
            _mutex.ReleaseMutex();
            _mutex.Close();
        }
        base.OnExit(e);
    }
}
