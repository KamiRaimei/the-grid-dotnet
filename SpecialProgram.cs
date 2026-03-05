// SpecialProgram.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public class SpecialProgram
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string ProgramType { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Creator { get; private set; }
        public double Energy { get; set; }
        public int Age { get; set; }
        public bool Active { get; set; }
        public Dictionary<string, Dictionary<string, object>> Functions { get; private set; }
        public Dictionary<string, object> Metadata { get; private set; }
        private static readonly Random _random = new Random();

        public SpecialProgram(string programId, string name, string programType, int x, int y, string creator = "USER")
        {
            Id = programId;
            Name = name;
            ProgramType = programType;
            X = x;
            Y = y;
            Creator = creator;
            Energy = 0.8;
            Age = 0;
            Active = true;
            Functions = InitializeFunctions();
            Metadata = new Dictionary<string, object>
            {
                ["created_at"] = DateTime.Now.ToString("o"),
                ["last_active"] = DateTime.Now.ToString("o"),
                ["success_count"] = 0,
                ["failure_count"] = 0,
                ["calculation_contributions"] = 0,
                ["total_calculation_power"] = 0.0
            };

            if (programType == "FIBONACCI_CALCULATOR")
            {
                Metadata["visual_effect"] = "calculation_pulse";
            }
        }

        private Dictionary<string, Dictionary<string, object>> InitializeFunctions()
        {
            var functions = new Dictionary<string, Dictionary<string, object>>();

            if (ProgramType == "SCANNER")
            {
                functions["scan_area"] = new Dictionary<string, object>
                {
                    ["range"] = 3,
                    ["cost"] = 0.1,
                    ["description"] = "Scan surrounding area for grid bugs"
                };
                functions["report_status"] = new Dictionary<string, object>
                {
                    ["range"] = 5,
                    ["cost"] = 0.05,
                    ["description"] = "Report on grid status in area"
                };
            }
            else if (ProgramType == "DEFENDER")
            {
                functions["quarantine_bug"] = new Dictionary<string, object>
                {
                    ["range"] = 2,
                    ["cost"] = 0.2,
                    ["description"] = "Quarantine nearby grid bugs"
                };
                functions["protect_cell"] = new Dictionary<string, object>
                {
                    ["range"] = 1,
                    ["cost"] = 0.15,
                    ["description"] = "Protect a specific cell from corruption"
                };
            }
            else if (ProgramType == "REPAIR")
            {
                functions["repair_cell"] = new Dictionary<string, object>
                {
                    ["range"] = 1,
                    ["cost"] = 0.25,
                    ["description"] = "Repair damaged cells"
                };
                functions["boost_energy"] = new Dictionary<string, object>
                {
                    ["range"] = 2,
                    ["cost"] = 0.3,
                    ["description"] = "Boost energy of nearby cells"
                };
            }
            else if (ProgramType == "SABOTEUR")
            {
                functions["disrupt_mcp"] = new Dictionary<string, object>
                {
                    ["range"] = 3,
                    ["cost"] = 0.4,
                    ["description"] = "Disrupt MCP programs in area"
                };
                functions["create_entropy"] = new Dictionary<string, object>
                {
                    ["range"] = 2,
                    ["cost"] = 0.35,
                    ["description"] = "Create controlled entropy"
                };
            }
            else if (ProgramType == "RECONFIGURATOR")
            {
                functions["reconfigure_cells"] = new Dictionary<string, object>
                {
                    ["range"] = 2,
                    ["cost"] = 0.3,
                    ["description"] = "Reconfigure cell types in area"
                };
                functions["optimize_grid"] = new Dictionary<string, object>
                {
                    ["range"] = 4,
                    ["cost"] = 0.5,
                    ["description"] = "Optimize grid layout"
                };
            }
            else if (ProgramType == "ENERGY_HARVESTER")
            {
                functions["harvest_energy"] = new Dictionary<string, object>
                {
                    ["range"] = 3,
                    ["cost"] = 0.1,
                    ["description"] = "Harvest energy from surroundings"
                };
                functions["distribute_energy"] = new Dictionary<string, object>
                {
                    ["range"] = 4,
                    ["cost"] = 0.2,
                    ["description"] = "Distribute energy to nearby cells"
                };
            }
            else if (ProgramType == "FIBONACCI_CALCULATOR")
            {
                functions["calculate_next"] = new Dictionary<string, object>
                {
                    ["range"] = 0,
                    ["cost"] = 0.2,
                    ["description"] = "Calculate next Fibonacci number using grid cooperation"
                };
                functions["optimize_calculation"] = new Dictionary<string, object>
                {
                    ["range"] = 3,
                    ["cost"] = 0.3,
                    ["description"] = "Optimize nearby calculation cells for Fibonacci computation"
                };
                functions["deploy_processors"] = new Dictionary<string, object>
                {
                    ["range"] = 2,
                    ["cost"] = 0.4,
                    ["description"] = "Deploy temporary Fibonacci processors to boost calculation"
                };
                functions["learning_analysis"] = new Dictionary<string, object>
                {
                    ["range"] = 0,
                    ["cost"] = 0.1,
                    ["description"] = "Analyze calculation efficiency and suggest improvements"
                };
            }

            return functions;
        }

        public (bool success, string message) ExecuteFunction(string functionName, GridCell[][] grid, int? targetX = null, int? targetY = null)
        {
            if (!Active || Energy <= 0)
                return (false, "Program inactive or out of energy");

            if (!Functions.ContainsKey(functionName))
                return (false, $"Function {functionName} not available");

            var func = Functions[functionName];
            double cost = Convert.ToDouble(func["cost"]);
            if (Energy < cost)
                return (false, "Insufficient energy");

            bool success = false;
            string resultMsg = "";

            if (ProgramType == "FIBONACCI_CALCULATOR")
            {
                if (functionName == "calculate_next")
                {
                    double contribution = Energy * 0.5;

                    for (int dy = -2; dy <= 2; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int nx = X + dx;
                            int ny = Y + dy;
                            if (nx >= 0 && nx < grid[0].Length && ny >= 0 && ny < grid.Length)
                            {
                                var cell = grid[ny][nx];
                                if (cell.CellType == CellType.MCP_PROGRAM || cell.CellType == CellType.FIBONACCI_PROCESSOR)
                                {
                                    cell.CalculationContribution += contribution * 0.1;
                                    cell.Processing = true;
                                }
                            }
                        }
                    }

                    Metadata["calculation_contributions"] = (int)Metadata["calculation_contributions"] + 1;
                    Metadata["total_calculation_power"] = (double)Metadata["total_calculation_power"] + contribution;

                    success = true;
                    resultMsg = $"Enhanced calculation initiated. Contributing {contribution:F2} power.";
                }
                else if (functionName == "deploy_processors")
                {
                    int deployed = 0;
                    (int dx, int dy)[] directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };

                    for (int i = 0; i < 3; i++)
                    {
                        var (dx, dy) = directions[_random.Next(directions.Length)];
                        int nx = X + dx;
                        int ny = Y + dy;
                        if (nx >= 0 && nx < grid[0].Length && ny >= 0 && ny < grid.Length)
                        {
                            if (grid[ny][nx].CellType == CellType.EMPTY)
                            {
                                var cell = new GridCell(CellType.FIBONACCI_PROCESSOR, 0.6);
                                cell.Metadata["temporary"] = true;
                                cell.Metadata["lifetime"] = 15;
                                cell.Metadata["deployed_by"] = Id;
                                grid[ny][nx] = cell;
                                deployed++;
                            }
                        }
                    }

                    success = deployed > 0;
                    resultMsg = $"Deployed {deployed} temporary Fibonacci processors.";
                }
            }

            // Visual feedback for successful execution
            if (success)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = X + dx;
                        int ny = Y + dy;
                        if (nx >= 0 && nx < grid[0].Length && ny >= 0 && ny < grid.Length)
                        {
                            var cell = grid[ny][nx];
                            if (cell.CellType != CellType.EMPTY)
                            {
                                cell.Metadata["recently_active"] = true;
                                cell.Metadata["active_timer"] = 5;
                            }
                        }
                    }
                }
            }

            if (success)
            {
                Energy -= cost;
                Metadata["success_count"] = (int)Metadata["success_count"] + 1;
                Metadata["last_active"] = DateTime.Now.ToString("o");
            }
            else
            {
                Metadata["failure_count"] = (int)Metadata["failure_count"] + 1;
            }

            return (success, resultMsg);
        }
    }
}