// Program.cs
using System;
using System.Windows.Forms;

namespace GridSimulation
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine("GRID SIMULATION - REINFORCEMENT LEARNING MCP AI");
            Console.WriteLine("Starting graphical interface...");
            Console.WriteLine("=".PadRight(70, '='));
            
            Application.Run(new MainForm());
        }
    }
}