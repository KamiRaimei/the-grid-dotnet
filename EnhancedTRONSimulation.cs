// EnhancedTRONSimulation.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GridSimulation
{
    public class EnhancedTRONSimulation
    {
        private bool _useCurses;
        public TRONGrid Grid { get; private set; }
        public EnhancedMCP MCP { get; private set; }
        public bool Running { get; set; }
        private DateTime _lastUpdate;
        private DateTime _mcpLastAction;
        public string UserInput { get; set; }
        private List<string> _inputBuffer;
        private List<string> _commandHistory;
        private int _historyIndex;
        public double SimulationSpeed { get; set; }
        private DateTime _lastSafeTime;
        private double _maxUpdateTime = 1.0;
        public bool ExitRequested { get; set; }
        private DateTime _lastLearningDisplay;
        private double _learningUpdateInterval = 5.0;
        private DateTime _lastFrameTime;
        private int _targetFps = 60;
        public string? LastMcpResponse { get; set; }
        private static readonly Random _random = new Random();

        public EnhancedTRONSimulation(bool useCurses = true)
        {
            _useCurses = useCurses;
            Grid = new TRONGrid(50, 30);
            MCP = new EnhancedMCP(Grid);
            Running = true;
            _lastUpdate = DateTime.Now;
            _mcpLastAction = DateTime.Now;
            UserInput = "";
            _inputBuffer = new List<string>();
            _commandHistory = new List<string>();
            _historyIndex = 0;
            SimulationSpeed = 1.0;
            _lastSafeTime = DateTime.Now;
            ExitRequested = false;
            _lastLearningDisplay = DateTime.Now;
            _lastFrameTime = DateTime.Now;
        }

        public void Run()
        {
            if (_useCurses)
            {
                // Note: Curses implementation would require a console library
                // For cross-platform, consider using Console directly
                Console.WriteLine("Curses mode is simulated using standard console.");
                FallbackMain();
            }
            else
            {
                FallbackMain();
            }
        }

        private void FallbackMain()
        {
            Console.Clear();
            Console.WriteLine("GRID SIMULATION - FIBONACCI SEQUENCE");
            Console.WriteLine("System Objective: Maintain Perfect Calculation Loop Through Learning");
            Console.WriteLine("=".PadRight(70, '='));

            try
            {
                while (Running)
                {
                    DateTime currentTime = DateTime.Now;

                    if ((currentTime - _lastUpdate).TotalSeconds >= 0.25 / SimulationSpeed)
                    {
                        Grid.Evolve();
                        _lastUpdate = currentTime;
                    }

                    if ((currentTime - _mcpLastAction).TotalSeconds >= 5.0 / SimulationSpeed)
                    {
                        MCP.AutonomousAction();
                        _mcpLastAction = currentTime;
                    }

                    FallbackDisplay();

                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        HandleFallbackInput(key);
                    }

                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\nError: {ex.Message}");
            }
            finally
            {
                // Save personality on exit
                MCP.LearningSystem?.SavePersonality();
                Console.WriteLine($"\nMCP personality saved");
                Console.WriteLine("Simulation terminated.");
            }
        }

        private void HandleFallbackInput(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Enter)
            {
                if (!string.IsNullOrWhiteSpace(UserInput))
                {
                    string command = UserInput.Trim();
                    _commandHistory.Add(command);
                    _historyIndex = _commandHistory.Count;

                    string response = MCP.ReceiveCommand(command);
                    LastMcpResponse = response;

                    if (command.ToLower() == "exit" || command.ToLower() == "quit" || command.ToLower() == "shutdown")
                    {
                        if (response.ToLower().Contains("shutdown") && response.ToLower().Contains("initiating"))
                        {
                            ExitRequested = true;
                            Running = false;
                            return;
                        }
                    }
                }
                UserInput = "";
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (UserInput.Length > 0)
                    UserInput = UserInput.Substring(0, UserInput.Length - 1);
            }
            else if (key.Key == ConsoleKey.UpArrow)
            {
                if (_commandHistory.Count > 0 && _historyIndex > 0)
                {
                    _historyIndex--;
                    UserInput = _commandHistory[_historyIndex];
                }
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                if (_commandHistory.Count > 0 && _historyIndex < _commandHistory.Count - 1)
                {
                    _historyIndex++;
                    UserInput = _commandHistory[_historyIndex];
                }
                else if (_historyIndex == _commandHistory.Count - 1)
                {
                    _historyIndex = _commandHistory.Count;
                    UserInput = "";
                }
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                MCP.LearningSystem?.SavePersonality();
                Running = false;
            }
            else if (key.KeyChar >= 32 && key.KeyChar <= 126)
            {
                UserInput += key.KeyChar;
            }
        }

        private void FallbackDisplay()
        {
            Console.Clear();
            
            Console.WriteLine("GRID SIMULATION - FIBONACCI SEQUENCE");
            Console.WriteLine($"Generation: {Grid.Generation:000000} | Status: {Grid.SystemStatus}");
            Console.WriteLine("System Objective: Maintain Perfect Calculation Loop Through Learning");
            Console.WriteLine("=".PadRight(70, '='));

            int displayWidth = Math.Min(60, Grid.Width);
            int displayHeight = Math.Min(15, Grid.Height);

            Console.WriteLine("+" + new string('-', displayWidth) + "+");

            for (int y = 0; y < displayHeight; y++)
            {
                Console.Write("|");
                for (int x = 0; x < displayWidth; x++)
                {
                    var cell = Grid.GetCell(x, y);
                    char ch = cell.GetChar();
                    
                    if (cell.Metadata.ContainsKey("energy_spark") && (bool)cell.Metadata["energy_spark"])
                        ch = '*';
                    else if (cell.Metadata.ContainsKey("recently_active") && (bool)cell.Metadata["recently_active"])
                        ch = cell.AnimationFrame % 2 == 0 ? '●' : '○';
                    
                    Console.Write(ch);
                }
                Console.WriteLine("|");
            }

            Console.WriteLine("+" + new string('-', displayWidth) + "+");

            Console.WriteLine("\n" + "=".PadRight(70, '='));
            Console.WriteLine("SYSTEM STATUS:");

            var stats = Grid.Stats;
            var calcStats = Grid.FibonacciCalculator.GetCalculationStats();

            string col1 = $"  User Programs: {stats.GetValueOrDefault("user_programs"):F0}\n" +
                          $"  MCP Programs:   {stats.GetValueOrDefault("mcp_programs"):F0}\n" +
                          $"  Grid Bugs:      {stats.GetValueOrDefault("grid_bugs"):F0}\n" +
                          $"  Special:        {stats.GetValueOrDefault("special_programs"):F0}\n" +
                          $"  Energy:         {stats.GetValueOrDefault("energy_level"):F2}\n" +
                          $"  Stability:      {stats.GetValueOrDefault("stability"):F2}";

            string col2 = $"  Entropy:        {stats.GetValueOrDefault("entropy"):F2}\n" +
                          $"  Loop Efficiency: {stats.GetValueOrDefault("loop_efficiency"):F2}\n" +
                          $"  Cell Cooperation:{stats.GetValueOrDefault("cell_cooperation"):F2}\n" +
                          $"  Calculation Rate:{calcStats.GetValueOrDefault("calculation_rate"):F2}/s\n" +
                          $"  Optimal State:   {stats.GetValueOrDefault("optimal_state"):F2}\n" +
                          $"  MCP State:       {MCP.State}";

            var col1Lines = col1.Split('\n');
            var col2Lines = col2.Split('\n');

            for (int i = 0; i < Math.Max(col1Lines.Length, col2Lines.Length); i++)
            {
                string line1 = i < col1Lines.Length ? col1Lines[i] : "";
                string line2 = i < col2Lines.Length ? col2Lines[i] : "";
                Console.WriteLine($"{line1,-30} {line2}");
            }

            Console.WriteLine("\n" + "-".PadRight(70, '-'));
            Console.WriteLine("FIBONACCI CALCULATION:");
            Console.WriteLine($"  Current: {calcStats.GetValueOrDefault("current_fibonacci_formatted")}");
            Console.WriteLine($"  Accumulator: {calcStats.GetValueOrDefault("accumulator"):F2}/1000.0");
            Console.WriteLine($"  Efficiency: {calcStats.GetValueOrDefault("efficiency_score"):F2}");
            Console.WriteLine($"  Optimization: {calcStats.GetValueOrDefault("optimization_level"):F2}");

            int calcCount = Grid.GetCalculatorCount();
            int fibProcessors = 0;
            for (int y = 0; y < Grid.Height; y++)
            {
                for (int x = 0; x < Grid.Width; x++)
                {
                    if (Grid.GetCell(x, y).CellType == CellType.FIBONACCI_PROCESSOR)
                        fibProcessors++;
                }
            }
            Console.WriteLine($"  Active Calculators: {calcCount} MCP + {fibProcessors} Processors");

            DateTime currentTime = DateTime.Now;
            if ((currentTime - _lastLearningDisplay).TotalSeconds >= _learningUpdateInterval)
            {
                var learningReport = MCP.LearningSystem.GetLearningReport();
                Console.WriteLine("\n" + "-".PadRight(70, '-'));
                Console.WriteLine("MCP LEARNING STATUS:");
                Console.WriteLine($"  Experiences: {learningReport.GetValueOrDefault("total_experiences")}");
                Console.WriteLine($"  Success Rate: {Convert.ToDouble(learningReport.GetValueOrDefault("success_rate", 0)) * 100:F1}%");
                Console.WriteLine($"  Learning Active: {MCP.State == MCPState.LEARNING}");
                _lastLearningDisplay = currentTime;
            }

            Console.WriteLine("\n" + "=".PadRight(70, '='));
            Console.WriteLine("MCP COMMUNICATION LOG (Last 4 entries):");
            Console.WriteLine("-".PadRight(70, '-'));

            var logEntries = MCP.Log.ToList();
            if (logEntries.Any())
            {
                foreach (string entry in logEntries.TakeLast(4))
                {
                    string displayEntry = entry.Length > 65 ? entry.Substring(0, 62) + "..." : entry;
                    Console.WriteLine($"  {displayEntry}");
                }
            }
            else
            {
                Console.WriteLine("  No log entries yet.");
            }

            Console.WriteLine("-".PadRight(70, '-'));

            if (!string.IsNullOrEmpty(MCP.LastAction))
            {
                string actionText = MCP.LastAction.Length > 65 ? MCP.LastAction.Substring(0, 62) + "..." : MCP.LastAction;
                Console.WriteLine($"\nLAST MCP ACTION: {actionText}");
            }

            Console.WriteLine("\n" + "=".PadRight(70, '='));
            if (MCP.WaitingForResponse)
            {
                Console.WriteLine("MCP is waiting for your response to a question.");
                Console.Write("YOUR RESPONSE> ");
            }
            else
            {
                Console.Write("MCP COMMAND> ");
            }

            Console.Write(UserInput);
            Console.Out.Flush();
        }
    }
}