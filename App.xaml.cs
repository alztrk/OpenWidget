using System;
using System.Windows;

namespace RigBadge;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            // Prevent silent crashes on UI thread
            args.Handled = true;
        };
    }
}
