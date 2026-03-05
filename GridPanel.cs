// GridPanel.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GridSimulation
{
    public class GridPanel : Panel
    {
        private EnhancedTRONSimulation simulation;
        private Font cellFont;
        private Font statusFont;
        private Brush[] colorBrushes;
        private Pen borderPen;

        public GridPanel(EnhancedTRONSimulation sim)
        {
            this.simulation = sim;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.BorderStyle = BorderStyle.FixedSingle;
            
            cellFont = new Font("Consolas", 12, FontStyle.Regular);
            statusFont = new Font("Segoe UI", 10, FontStyle.Bold);
            borderPen = new Pen(Color.FromArgb(100, 100, 120), 1);
            
            // Initialize color brushes for different cell types
            colorBrushes = new Brush[]
            {
                new SolidBrush(Color.FromArgb(40, 40, 50)),      // EMPTY
                new SolidBrush(Color.FromArgb(0, 120, 215)),     // USER_PROGRAM - Blue
                new SolidBrush(Color.FromArgb(232, 17, 35)),     // MCP_PROGRAM - Red
                new SolidBrush(Color.FromArgb(40, 180, 40)),     // GRID_BUG - Green
                new SolidBrush(Color.White),                      // ISO_BLOCK - White
                new SolidBrush(Color.FromArgb(255, 200, 0)),     // ENERGY_LINE - Yellow
                new SolidBrush(Color.FromArgb(0, 200, 200)),     // DATA_STREAM - Cyan
                new SolidBrush(Color.FromArgb(200, 0, 200)),     // SYSTEM_CORE - Magenta
                new SolidBrush(Color.FromArgb(0, 255, 255)),     // SPECIAL_PROGRAM - Bright Cyan
                new SolidBrush(Color.FromArgb(255, 255, 100))    // FIBONACCI_PROCESSOR - Bright Yellow
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int cellSize = 16;
            int startX = 10;
            int startY = 10;

            // Draw grid background
            g.Clear(this.BackColor);

            // Draw title
            g.DrawString("GRID STATUS - Fibonacci Calculation Grid", 
                new Font("Segoe UI", 12, FontStyle.Bold), 
                Brushes.LightGray, startX, 5);

            // Draw cells
            for (int y = 0; y < simulation.Grid.Height; y++)
            {
                for (int x = 0; x < simulation.Grid.Width; x++)
                {
                    var cell = simulation.Grid.GetCell(x, y);
                    if (cell == null) continue;

                    Rectangle cellRect = new Rectangle(
                        startX + x * cellSize,
                        startY + y * cellSize + 20,
                        cellSize - 1,
                        cellSize - 1
                    );

                    // Draw cell background
                    int colorIndex = (int)cell.CellType;
                    if (colorIndex >= 0 && colorIndex < colorBrushes.Length)
                    {
                        g.FillRectangle(colorBrushes[colorIndex], cellRect);
                    }

                    // Draw cell character
                    string cellChar = cell.GetChar().ToString();
                    
                    // Add glow effect for processing cells
                    if (cell.Processing)
                    {
                        using (Pen glowPen = new Pen(Color.Yellow, 2))
                        {
                            g.DrawRectangle(glowPen, cellRect);
                        }
                    }

                    // Draw the character
                    using (Brush textBrush = new SolidBrush(Color.White))
                    {
                        g.DrawString(cellChar, cellFont, textBrush, 
                            cellRect.X + 2, cellRect.Y);
                    }

                    // Draw grid lines
                    g.DrawRectangle(borderPen, cellRect);
                }
            }

            // Draw status line
            string status = $"Generation: {simulation.Grid.Generation:000000} | " +
                           $"Status: {simulation.Grid.SystemStatus} | " +
                           $"MCP State: {simulation.MCP.State}";
            g.DrawString(status, statusFont, Brushes.LightGreen, 
                startX, startY + simulation.Grid.Height * cellSize + 25);
        }
    }
}