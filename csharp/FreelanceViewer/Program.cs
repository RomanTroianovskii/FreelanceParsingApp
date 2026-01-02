using Avalonia;
using System;

namespace FreelanceViewer
{
    class Program
    {
        // Initialization code. Don't use any Avalonia XAML here.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}