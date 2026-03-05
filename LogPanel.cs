// LogPanel.cs - Add at the top
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GridSimulation
{
    public class LogPanel : Panel
    {
        private EnhancedTRONSimulation simulation;
        private Font titleFont;
        private Font logFont;
        private ListBox logListBox;

        public LogPanel(EnhancedTRONSimulation sim)
        {
            this.simulation = sim;
            
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.BorderStyle = BorderStyle.FixedSingle;

            titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            logFont = new Font("Consolas", 9, FontStyle.Regular);

            // Create log listbox
            logListBox = new ListBox
            {
                Location = new Point(10, 35),
                Size = new Size(this.Width - 25, this.Height - 50),
                BackColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.LightGray,
                Font = logFont,
                BorderStyle = BorderStyle.None,
                ScrollAlwaysVisible = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            this.Controls.Add(logListBox);
        }

        public void UpdateLog()
        {
            var logs = simulation.MCP.Log.ToList();
            logListBox.Items.Clear();
            
            foreach (var log in logs)
            {
                logListBox.Items.Add(log);
            }

            // Auto-scroll to bottom
            if (logListBox.Items.Count > 0)
                logListBox.TopIndex = logListBox.Items.Count - 1;
        }

        public void AddMessage(string source, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string entry = $"[{timestamp}] {source}: {message}";
            
            logListBox.Items.Add(entry);
            if (logListBox.Items.Count > 100)
                logListBox.Items.RemoveAt(0);
            
            logListBox.TopIndex = logListBox.Items.Count - 1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            
            g.DrawString("MCP COMMUNICATION LOG", titleFont, Brushes.Cyan, 10, 10);
            
            // Update listbox size if needed
            if (logListBox.Width != this.Width - 25 || logListBox.Height != this.Height - 50)
            {
                logListBox.Size = new Size(this.Width - 25, this.Height - 50);
            }
        }
    }
}