// CommandPanel.cs - Add at the top
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GridSimulation
{
    public class CommandPanel : Panel
    {
        private EnhancedTRONSimulation simulation;
        private Font titleFont;
        private Font labelFont;
        private TextBox commandTextBox = null!;
        private Button sendButton = null!;
        private RichTextBox responseTextBox = null!;
        private ListBox suggestionsListBox = null!;
        private List<string> commandHistory;
        private int historyIndex;

        public CommandPanel(EnhancedTRONSimulation sim)
        {
            this.simulation = sim;
            this.commandHistory = new List<string>();
            this.historyIndex = 0;
            
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(25, 25, 35);
            this.BorderStyle = BorderStyle.FixedSingle;

            titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            labelFont = new Font("Segoe UI", 9, FontStyle.Regular);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Command input
            Label cmdLabel = new Label
            {
                Text = "COMMAND:",
                Location = new Point(10, 15),
                Size = new Size(70, 25),
                ForeColor = Color.LightGray,
                Font = labelFont
            };

            commandTextBox = new TextBox
            {
                Location = new Point(90, 12),
                Size = new Size(350, 25),
                BackColor = Color.FromArgb(40, 40, 50),
                ForeColor = Color.White,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            commandTextBox.KeyDown += CommandTextBox_KeyDown;

            sendButton = new Button
            {
                Text = "SEND",
                Location = new Point(500, 11),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            sendButton.Click += SendButton_Click;

            // Suggestions list
            Label suggestLabel = new Label
            {
                Text = "SUGGESTIONS:",
                Location = new Point(15, 50),
                Size = new Size(90, 20),
                ForeColor = Color.LightGray,
                Font = labelFont
            };

            suggestionsListBox = new ListBox
            {
                Location = new Point(15, 75),
                Size = new Size(565, 70),
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.LightGreen,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9)
            };
            suggestionsListBox.SelectedIndexChanged += SuggestionsListBox_SelectedIndexChanged;
            suggestionsListBox.MouseDoubleClick += SuggestionsListBox_MouseDoubleClick;

            // Response area
            Label responseLabel = new Label
            {
                Text = "MCP RESPONSE:",
                Location = new Point(15, 155),
                Size = new Size(120, 20),
                ForeColor = Color.LightGray,
                Font = labelFont
            };

            responseTextBox = new RichTextBox
            {
                Location = new Point(15, 180),
                Size = new Size(565, 100),
                BackColor = Color.FromArgb(20, 20, 30),
                ForeColor = Color.Cyan,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Add controls
            this.Controls.Add(cmdLabel);
            this.Controls.Add(commandTextBox);
            this.Controls.Add(sendButton);
            this.Controls.Add(suggestLabel);
            this.Controls.Add(suggestionsListBox);
            this.Controls.Add(responseLabel);
            this.Controls.Add(responseTextBox);

            // Populate suggestions
            UpdateSuggestions();
        }

        private void CommandTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendCommand();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (commandHistory.Count > 0 && historyIndex > 0)
                {
                    historyIndex--;
                    commandTextBox.Text = commandHistory[historyIndex];
                    commandTextBox.SelectionStart = commandTextBox.Text.Length;
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (commandHistory.Count > 0 && historyIndex < commandHistory.Count - 1)
                {
                    historyIndex++;
                    commandTextBox.Text = commandHistory[historyIndex];
                }
                else if (historyIndex == commandHistory.Count - 1)
                {
                    historyIndex = commandHistory.Count;
                    commandTextBox.Text = "";
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Tab)
            {
                // Auto-complete
                if (!string.IsNullOrWhiteSpace(commandTextBox.Text))
                {
                    string input = commandTextBox.Text.ToLower();
                    foreach (string suggestion in GetCommonCommands())
                    {
                        if (suggestion.ToLower().StartsWith(input))
                        {
                            commandTextBox.Text = suggestion;
                            commandTextBox.SelectionStart = commandTextBox.Text.Length;
                            break;
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private void SendButton_Click(object? sender, EventArgs e)
        {
            SendCommand();
        }

        private void SuggestionsListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (suggestionsListBox.SelectedItem != null)
            {
                commandTextBox.Text = suggestionsListBox.SelectedItem.ToString();
                commandTextBox.Focus();
                commandTextBox.SelectionStart = commandTextBox.Text.Length;
            }
        }

        private void SuggestionsListBox_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (suggestionsListBox.SelectedItem != null)
            {
                commandTextBox.Text = suggestionsListBox.SelectedItem.ToString();
                SendCommand();
            }
        }

        private void SendCommand()
        {
            string command = commandTextBox.Text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            // Add to history
            commandHistory.Add(command);
            historyIndex = commandHistory.Count;

            try
            {
                // Process command through MCP
                string response = simulation.MCP.ReceiveCommand(command);
                
                // Update response with formatting
                responseTextBox.Clear();
                
                // Add user command in green
                responseTextBox.SelectionColor = Color.LightGreen;
                responseTextBox.SelectionFont = new Font("Consolas", 10, FontStyle.Bold);
                responseTextBox.AppendText($"> {command}\n\n");
                
                // Add MCP response in cyan
                responseTextBox.SelectionColor = Color.Cyan;
                responseTextBox.SelectionFont = new Font("Consolas", 10);
                responseTextBox.AppendText($"MCP: {response}");
                
                // Scroll to top
                responseTextBox.SelectionStart = 0;
                responseTextBox.ScrollToCaret();
            }
            catch (Exception ex)
            {
                responseTextBox.Text = $"Error processing command: {ex.Message}";
            }

            // Clear input
            commandTextBox.Text = "";

            // Update suggestions based on command
            UpdateSuggestions();
        }

        private void UpdateSuggestions()
        {
            suggestionsListBox.Items.Clear();
            foreach (string cmd in GetCommonCommands())
            {
                suggestionsListBox.Items.Add(cmd);
            }
        }

        private string[] GetCommonCommands()
        {
            return new[]
            {
                "status",
                "help",
                "scan",
                "repair",
                "boost energy",
                "loop efficiency",
                "optimize loop",
                "learning_status",
                "perfect loop",
                "cell cooperation",
                "create fibonacci_calculator named FibMaster",
                "deploy processors",
                "calculate fibonacci",
                "delete cell at 10,20",
                "repurpose cell at 15,25 to FIBONACCI_PROCESSOR",
                "optimize cells",
                "who are you",
                "why did you do that"
            };
        }
    }
}