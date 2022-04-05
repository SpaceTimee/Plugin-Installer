using System.Windows;

namespace Vizpower_Plugin_Installer_WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow(e.Args).Show();
        }
    }
}