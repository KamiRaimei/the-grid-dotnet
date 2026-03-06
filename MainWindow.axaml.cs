// MainWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;

namespace GridSimulation
{
    public partial class MainWindow : Window
    {
        private EnhancedTRONSimulation simulation;
        private Timer? updateTimer;
        private Timer? animationTimer;
        private ObservableCollection<string> logEntries;
        private ObservableCollection<GridCellViewModel> gridCells;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize simulation
            simulation = new EnhancedTRONSimulation(useCurses: false);
            
            // Initialize collections
            logEntries = new ObservableCollection<string>();
            gridCells = new ObservableCollection<GridCellViewModel>();
            
            // Set up bindings
            if (LogListBox != null)
                LogListBox.ItemsSource = logEntries;
            
            // Set up event handlers
            if (SendButton != null)
                SendButton.Click += SendButton_Click;
            
            if (CommandTextBox != null)
                CommandTextBox.KeyDown += CommandTextBox_KeyDown;
            
            // Start timers
            StartUpdateTimer();
            StartAnimationTimer();
            
            // Initial update
            UpdateDisplay();
        }

        private void StartUpdateTimer()
        {
            updateTimer = new Timer(250); // 4 FPS for simulation
            updateTimer.Elapsed += (s, e) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    simulation.Grid.Evolve();
                    simulation.MCP.AutonomousAction();
                    UpdateDisplay();
                });
            };
            updateTimer.AutoReset = true;
            updateTimer.Start();
        }

        private void StartAnimationTimer()
        {
            animationTimer = new Timer(33); // ~30 FPS for smooth animation
            animationTimer.Elapsed += (s, e) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateGridView();
                });
            };
            animationTimer.AutoReset = true;
            animationTimer.Start();
        }

        private void UpdateDisplay()
        {
            UpdateStats();
            UpdateLog();
            UpdateStatusText();
        }

        private void UpdateGridView()
        {
            if (GridItemsControl == null) return;
            
            gridCells.Clear();
            for (int y = 0; y < simulation.Grid.Height; y++)
            {
                for (int x = 0; x < simulation.Grid.Width; x++)
                {
                    var cell = simulation.Grid.GetCell(x, y);
                    if (cell != null)
                    {
                        gridCells.Add(new GridCellViewModel(cell));
                    }
                }
            }
            GridItemsControl.ItemsSource = gridCells;
        }

        private void UpdateStats()
        {
            var stats = simulation.Grid.Stats;
            var calcStats = simulation.Grid.FibonacciCalculator.GetCalculationStats();

            if (UserProgramsText != null)
                UserProgramsText.Text = $"User Programs: {GetDouble(stats, "user_programs"):F0}";
            
            if (MCPProgramsText != null)
                MCPProgramsText.Text = $"MCP Programs: {GetDouble(stats, "mcp_programs"):F0}";
            
            if (GridBugsText != null)
                GridBugsText.Text = $"Grid Bugs: {GetDouble(stats, "grid_bugs"):F0}";
            
            if (SpecialProgramsText != null)
                SpecialProgramsText.Text = $"Special Programs: {GetDouble(stats, "special_programs"):F0}";
            
            if (EnergyLevelText != null)
                EnergyLevelText.Text = $"Energy Level: {GetDouble(stats, "energy_level"):F2}";
            
            if (StabilityText != null)
                StabilityText.Text = $"Stability: {GetDouble(stats, "stability"):F2}";
            
            if (LoopEfficiencyText != null)
                LoopEfficiencyText.Text = $"Loop Efficiency: {GetDouble(stats, "loop_efficiency"):F2}";
            
            if (CellCooperationText != null)
                CellCooperationText.Text = $"Cell Cooperation: {GetDouble(stats, "cell_cooperation"):F2}";
            
            double calcRate = GetDoubleFromObject(calcStats, "calculation_rate");
            if (CalculationRateText != null)
                CalculationRateText.Text = $"Calculation Rate: {calcRate:F2}/s";
            
            if (MCPStateText != null)
                MCPStateText.Text = $"MCP State: {simulation.MCP.State}";

            if (LoopEfficiencyBar != null)
                LoopEfficiencyBar.Value = GetDouble(stats, "loop_efficiency");
            
            if (CalculationRateBar != null)
                CalculationRateBar.Value = Math.Min(calcRate, 500);
        }

        private double GetDouble(System.Collections.Generic.Dictionary<string, double> dict, string key, double defaultValue = 0)
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }

        private double GetDoubleFromObject(System.Collections.Generic.Dictionary<string, object> dict, string key, double defaultValue = 0)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                try
                {
                    return Convert.ToDouble(dict[key]);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private void UpdateLog()
        {
            if (LogListBox == null) return;
            
            var logs = simulation.MCP.Log.ToList();
            logEntries.Clear();
            foreach (var log in logs.TakeLast(10))
            {
                logEntries.Add(log);
            }
        }

        private void UpdateStatusText()
        {
            if (StatusText != null)
                StatusText.Text = $"Generation: {simulation.Grid.Generation:000000} | Status: {simulation.Grid.SystemStatus}";
        }

        private void SendButton_Click(object? sender, RoutedEventArgs e)
        {
            SendCommand();
        }

        private void CommandTextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                SendCommand();
                e.Handled = true;
            }
        }

        private void SendCommand()
        {
            if (CommandTextBox == null || ResponseText == null) return;
            
            string command = CommandTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(command)) return;

            try
            {
                string response = simulation.MCP.ReceiveCommand(command);
                ResponseText.Text = $"> {command}\nMCP: {response}";
                ResponseText.Foreground = new SolidColorBrush(Colors.Cyan);
                CommandTextBox.Text = "";
                
                // Update log immediately
                UpdateLog();
            }
            catch (Exception ex)
            {
                ResponseText.Text = $"Error: {ex.Message}";
                ResponseText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            updateTimer?.Stop();
            updateTimer?.Dispose();
            
            animationTimer?.Stop();
            animationTimer?.Dispose();
            
            simulation.MCP.LearningSystem?.SavePersonality();
            simulation.Running = false;
            
            base.OnClosed(e);
        }
    }
}