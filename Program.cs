// Program.cs
using Avalonia;
using System;

namespace GridSimulation
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine("GRID SIMULATION - REINFORCEMENT LEARNING MCP AI");
            Console.WriteLine($"Platform: {GetPlatformInfo()}");
            Console.WriteLine($"Personality file: mcp_personality.json");
            Console.WriteLine("=".PadRight(70, '='));

            SetupPlatformPaths();
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static string GetPlatformInfo()
        {
            if (OperatingSystem.IsWindows())
                return "Windows";
            else if (OperatingSystem.IsLinux())
                return "Linux";
            else if (OperatingSystem.IsMacOS())
                return "macOS";
            else
                return "Unknown";
        }

        private static void SetupPlatformPaths()
        {
            string basePath;
            
            if (OperatingSystem.IsWindows())
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                basePath = System.IO.Path.Combine(basePath, "GridSimulation");
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                basePath = System.IO.Path.Combine(basePath, ".gridsimulation");
            }
            else
            {
                basePath = System.IO.Directory.GetCurrentDirectory();
            }

            System.IO.Directory.CreateDirectory(basePath);
            Environment.CurrentDirectory = basePath;
            
            Console.WriteLine($"Data directory: {basePath}");
        }
    }
}