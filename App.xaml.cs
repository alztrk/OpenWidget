using System;
using System.Windows;

namespace OpenWidget;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            try
            {
                System.IO.File.WriteAllText("D:\\projects\\OpenWidget\\crash.log", args.Exception.ToString());
            }
            catch { }
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try
            {
                System.IO.File.WriteAllText("D:\\projects\\OpenWidget\\crash_appdomain.log", args.ExceptionObject?.ToString() ?? "Unknown crash");
            }
            catch { }
        };
    }
}
