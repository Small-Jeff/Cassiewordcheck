using System.Windows;

namespace CassieWordCheck;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Enable modern styling
        FrameworkElement.StyleProperty.OverrideMetadata(
            typeof(Window),
            new FrameworkPropertyMetadata(null));
    }
}
