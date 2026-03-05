// StatsPanel.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GridSimulation
{
    public class StatsPanel : Panel
    {
        private EnhancedTRONSimulation simulation;
        private Font titleFont;
        private Font statFont;
        private Brush progressBarBrush;

        public StatsPanel(EnhancedTRONSimulation sim)
        {
            this.simulation = sim;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(25, 25, 35);
            this.BorderStyle = BorderStyle.FixedSingle;
            
            titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            statFont = new Font("Consolas", 10, FontStyle.Regular);
            progressBarBrush = new SolidBrush(Color.FromArgb(0, 200, 100));
        }

        public void UpdateStats()
        {
            this.Invalidate();
        }

        // Helper methods to safely get values from dictionaries
        private double GetDouble(Dictionary<string, double> dict, string key, double defaultValue = 0)
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }

        private double GetDoubleFromObject(Dictionary<string, object> dict, string key, double defaultValue = 0)
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

        private string GetStringFromObject(Dictionary<string, object> dict, string key, string defaultValue = "")
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                return dict[key]?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int x = 15;
            int y = 15;

            // Title
            g.DrawString("SYSTEM STATISTICS", titleFont, Brushes.Cyan, x, y);
            y += 30;

            var stats = simulation.Grid.Stats;
            var calcStats = simulation.Grid.FibonacciCalculator.GetCalculationStats();

            // Column 1
            int col1X = x;
            DrawStatLine(g, col1X, ref y, "User Programs:", GetDouble(stats, "user_programs").ToString("F0"));
            DrawStatLine(g, col1X, ref y, "MCP Programs:", GetDouble(stats, "mcp_programs").ToString("F0"));
            DrawStatLine(g, col1X, ref y, "Grid Bugs:", GetDouble(stats, "grid_bugs").ToString("F0"));
            DrawStatLine(g, col1X, ref y, "Special Programs:", GetDouble(stats, "special_programs").ToString("F0"));
            DrawStatLine(g, col1X, ref y, "Energy Level:", $"{GetDouble(stats, "energy_level"):F2}");
            DrawStatLine(g, col1X, ref y, "Stability:", $"{GetDouble(stats, "stability"):F2}");

            // Column 2
            y = 45;
            int col2X = 220;
            DrawStatLine(g, col2X, ref y, "Entropy:", $"{GetDouble(stats, "entropy"):F2}");
            DrawStatLine(g, col2X, ref y, "Loop Efficiency:", $"{GetDouble(stats, "loop_efficiency"):F2}");
            DrawStatLine(g, col2X, ref y, "Cell Cooperation:", $"{GetDouble(stats, "cell_cooperation"):F2}");
            DrawStatLine(g, col2X, ref y, "Calculation Rate:", $"{GetDoubleFromObject(calcStats, "calculation_rate"):F2}/s");
            DrawStatLine(g, col2X, ref y, "Optimal State:", $"{GetDouble(stats, "optimal_state"):F2}");
            DrawStatLine(g, col2X, ref y, "User Resistance:", $"{GetDouble(stats, "user_resistance"):F2}");

            // Progress bars
            y = 170;
            DrawProgressBar(g, x, y, "Loop Efficiency", GetDouble(stats, "loop_efficiency"));
            DrawProgressBar(g, x + 250, y, "Calculation Rate", 
                Math.Min(1.0, GetDoubleFromObject(calcStats, "calculation_rate", 0) / 500));

            // Fibonacci info
            y = 210;
            g.DrawString("FIBONACCI CALCULATION", titleFont, Brushes.Yellow, x, y);
            y += 25;
            
            string fibNum = GetStringFromObject(calcStats, "current_fibonacci_formatted", "0");
            if (fibNum.Length > 30)
                fibNum = fibNum.Substring(0, 30) + "...";
            
            g.DrawString($"Current: {fibNum}", statFont, Brushes.White, x, y);
            g.DrawString($"Efficiency: {GetDoubleFromObject(calcStats, "efficiency_score"):F2}", 
                statFont, Brushes.White, x + 200, y);
        }

        private void DrawStatLine(Graphics g, int x, ref int y, string label, string value)
        {
            g.DrawString(label, statFont, Brushes.LightGray, x, y);
            g.DrawString(value, statFont, Brushes.White, x + 130, y);
            y += 20;
        }

        private void DrawProgressBar(Graphics g, int x, int y, string label, double value)
        {
            g.DrawString(label, statFont, Brushes.LightGray, x, y - 15);
            
            Rectangle barRect = new Rectangle(x, y, 200, 15);
            g.DrawRectangle(Pens.Gray, barRect);
            
            int fillWidth = (int)(200 * Math.Max(0, Math.Min(1, value)));
            Rectangle fillRect = new Rectangle(x, y, fillWidth, 15);
            g.FillRectangle(progressBarBrush, fillRect);
            
            g.DrawString($"{value:P0}", statFont, Brushes.White, x + 210, y);
        }
    }
}