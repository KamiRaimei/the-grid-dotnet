// MainForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GridSimulation
{
    public class MainForm : Form
    {
        private GridPanel gridPanel = null!;
        private StatsPanel statsPanel = null!;
        private LogPanel logPanel = null!;
        private CommandPanel commandPanel = null!;
        private System.Windows.Forms.Timer updateTimer = null!;
        private System.Windows.Forms.Timer animationTimer = null!;
        private System.Windows.Forms.Timer autoSaveTimer = null!;
        private EnhancedTRONSimulation simulation = null!;

        public MainForm()
        {
            this.Text = "GRID SIMULATION - Fibonacci Sequence MCP AI";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 40);
            this.FormClosing += MainForm_FormClosing;

            // Initialize simulation
            simulation = new EnhancedTRONSimulation(useCurses: false);
            
            // Create panels
            gridPanel = new GridPanel(simulation);
            statsPanel = new StatsPanel(simulation);
            logPanel = new LogPanel(simulation);
            commandPanel = new CommandPanel(simulation);

            // Position panels
            gridPanel.Location = new Point(20, 20);
            gridPanel.Size = new Size(800, 500);

            statsPanel.Location = new Point(840, 20);
            statsPanel.Size = new Size(540, 250);

            logPanel.Location = new Point(840, 280);
            logPanel.Size = new Size(540, 400);

            commandPanel.Location = new Point(20, 540);
            commandPanel.Size = new Size(1360, 300);

            // Add panels to form
            this.Controls.Add(gridPanel);
            this.Controls.Add(statsPanel);
            this.Controls.Add(logPanel);
            this.Controls.Add(commandPanel);

            // Setup update timer (simulation logic)
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 250; // 250ms = 4 FPS for simulation
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            // Setup animation timer (smooth 30 FPS)
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 33; // ~30 FPS
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Auto-save timer
            autoSaveTimer = new System.Windows.Forms.Timer();
            autoSaveTimer.Interval = 300000; // 5 minutes
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            autoSaveTimer.Start();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            // Run simulation logic
            simulation.Grid.Evolve();
            simulation.MCP.AutonomousAction();
            
            // Update displays
            statsPanel.UpdateStats();
            logPanel.UpdateLog();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            // Smooth animation refresh
            gridPanel.Invalidate();
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            simulation.MCP.LearningSystem?.SavePersonality();
            logPanel.AddMessage("System", "Auto-save completed.");
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Clean up
            updateTimer.Stop();
            animationTimer.Stop();
            autoSaveTimer.Stop();
            
            // Save personality
            simulation.MCP.LearningSystem?.SavePersonality();
            
            simulation.Running = false;
        }
    }
}