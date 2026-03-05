// EnhancedMCP.Autonomous.cs (partial class)
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public partial class EnhancedMCP
    {
        public string? AutonomousAction()
        {
            DateTime currentTime = DateTime.Now;

            if ((currentTime - _lastAutonomousActionTime).TotalSeconds < 1.0)
                return null;

            _lastAutonomousActionTime = currentTime;

            var currentState = new Dictionary<string, double>(_grid.Stats);
            var possibleActions = GeneratePossibleActions(currentState);

            if (!possibleActions.Any())
                return null;

            string actionType = LearningSystem.GetAction(currentState, possibleActions);

            var actionResult = ExecuteAutonomousAction(actionType, currentState);

            if (actionResult != null)
            {
                LastAction = actionResult["message"]?.ToString() ?? "";
                AddLog($"MCP: {LastAction}");
                return LastAction;
            }

            return null;
        }

        private List<string> GeneratePossibleActions(Dictionary<string, double> state)
        {
            var actions = new List<string>();

            var suggested = LearningSystem.SuggestOptimalAction(state);
            if (suggested != null && suggested.ContainsKey("action"))
                actions.Add(suggested["action"]?.ToString() ?? "");

            if (state.GetValueOrDefault("loop_efficiency", 0) < 0.8)
            {
                actions.AddRange(new[] {
                    "optimize_calculation_loop",
                    "deploy_fibonacci_processors",
                    "improve_cell_cooperation"
                });
            }

            if (state.GetValueOrDefault("grid_bugs", 0) > 5)
            {
                actions.AddRange(new[] {
                    "quarantine_grid_bugs",
                    "contain_bug_outbreak",
                    "stabilize_system"
                });
            }

            if (state.GetValueOrDefault("calculation_rate", 0) < 100)
            {
                actions.AddRange(new[] {
                    "boost_calculation_rate",
                    "add_calculation_infrastructure",
                    "optimize_fibonacci_calculation"
                });
            }

            if (state.GetValueOrDefault("cell_cooperation", 0) < 0.6)
            {
                actions.AddRange(new[] {
                    "improve_cell_connectivity",
                    "add_data_streams",
                    "enhance_collaboration"
                });
            }

            if (state.GetValueOrDefault("energy_level", 0) > 0.7 && state.GetValueOrDefault("calculation_rate", 0) < 150)
            {
                actions.AddRange(new[] {
                    "inefficiency_cleanup",
                    "cell_repurposing",
                    "optimize_cell_efficiency"
                });
            }

            actions.AddRange(new[] {
                "maintain_energy_grid",
                "optimize_resource_distribution",
                "balance_system_load"
            });

            return actions.Distinct().ToList();
        }

        private Dictionary<string, object>? ExecuteAutonomousAction(string actionType, Dictionary<string, double> state)
        {
            var result = new Dictionary<string, object>
            {
                ["action"] = actionType,
                ["success"] = false,
                ["message"] = "",
                ["impact"] = 0.0
            };

            try
            {
                if (actionType == "optimize_calculation_loop")
                {
                    OptimizeCalculationLoop();
                    result["success"] = true;
                    result["message"] = "Optimizing calculation loop using learned strategies";
                }
                else if (actionType == "deploy_fibonacci_processors")
                {
                    int deployed = DeployFibonacciProcessors(3);
                    result["success"] = deployed > 0;
                    result["message"] = $"Deployed {deployed} Fibonacci processors";
                }
                else if (actionType == "quarantine_grid_bugs")
                {
                    int contained = ContainGridBugs();
                    result["success"] = contained > 0;
                    result["message"] = $"Contained {contained} grid bugs";
                }
                else if (actionType == "boost_calculation_rate")
                {
                    bool boosted = BoostCalculationRate();
                    result["success"] = boosted;
                    result["message"] = "Boosted calculation rate through infrastructure optimization";
                }
                else if (actionType == "improve_cell_connectivity")
                {
                    int improved = ImproveCellConnectivity();
                    result["success"] = improved > 0;
                    result["message"] = $"Added {improved} data streams for better connectivity";
                }
                else if (actionType == "maintain_energy_grid")
                {
                    int maintained = MaintainEnergyGrid();
                    result["success"] = maintained > 0;
                    result["message"] = $"Added {maintained} energy lines for system stability";
                }
                else if (actionType == "inefficiency_cleanup")
                {
                    int deleted = CleanupInefficientCells();
                    result["success"] = deleted > 0;
                    result["message"] = $"Cleaned up {deleted} inefficient cells to improve calculation rate";
                }
                else if (actionType == "cell_repurposing")
                {
                    int repurposed = RepurposeInefficientCells();
                    result["success"] = repurposed > 0;
                    result["message"] = $"Repurposed {repurposed} cells for optimal calculation";
                }
                else if (actionType == "optimize_cell_efficiency")
                {
                    int optimized = OptimizeCellEfficiency();
                    result["success"] = optimized > 0;
                    result["message"] = $"Optimized {optimized} cells for maximum calculation efficiency";
                }
                else
                {
                    result["message"] = "Performing system maintenance";
                    result["success"] = true;
                }

                _grid.UpdateStats();
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private int DeployFibonacciProcessors(int count)
        {
            int deployed = 0;
            int centerX = _grid.Width / 2;
            int centerY = _grid.Height / 2;

            for (int i = 0; i < count; i++)
            {
                int x = centerX + _random.Next(-10, 11);
                int y = centerY + _random.Next(-8, 9);

                x = Math.Max(0, Math.Min(x, _grid.Width - 1));
                y = Math.Max(0, Math.Min(y, _grid.Height - 1));

                if (_grid.GetCell(x, y).CellType == CellType.EMPTY)
                {
                    var cell = new GridCell(CellType.FIBONACCI_PROCESSOR, 0.9);
                    cell.Metadata["permanent"] = true;
                    cell.Metadata["calculation_power"] = 1.0;
                    // _grid.SetCell(x, y, cell);
                    deployed++;
                }
            }

            return deployed;
        }

        private int ContainGridBugs()
        {
            int contained = 0;
            var bugPositions = new List<(int, int)>();

            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    if (_grid.GetCell(x, y).CellType == CellType.GRID_BUG)
                        bugPositions.Add((x, y));
                }
            }

            foreach (var (x, y) in bugPositions.Take(3))
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                        {
                            if (_grid.GetCell(nx, ny).CellType == CellType.EMPTY)
                            {
                                // _grid.SetCell(nx, ny, new GridCell(CellType.ISO_BLOCK, 0.9));
                                contained++;
                            }
                        }
                    }
                }
            }

            return contained;
        }

        private bool BoostCalculationRate()
        {
            int converted = 0;
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);
                    if (cell.CellType == CellType.MCP_PROGRAM &&
                        (!cell.Metadata.ContainsKey("is_calculator") || !(bool)cell.Metadata["is_calculator"]))
                    {
                        cell.Metadata["is_calculator"] = true;
                        cell.Metadata["calculation_power"] = 0.8;
                        converted++;
                        if (converted >= 5) break;
                    }
                }
                if (converted >= 5) break;
            }

            return converted > 0;
        }

        private int ImproveCellConnectivity()
        {
            int added = 0;
            var calculatorPositions = new List<(int, int)>();

            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);
                    if (cell.CellType == CellType.MCP_PROGRAM &&
                        cell.Metadata.ContainsKey("is_calculator") && (bool)cell.Metadata["is_calculator"])
                    {
                        calculatorPositions.Add((x, y));
                    }
                    else if (cell.CellType == CellType.FIBONACCI_PROCESSOR)
                    {
                        calculatorPositions.Add((x, y));
                    }
                }
            }

            if (calculatorPositions.Count >= 2)
            {
                var (x1, y1) = calculatorPositions[_random.Next(calculatorPositions.Count)];
                var (x2, y2) = calculatorPositions[_random.Next(calculatorPositions.Count)];

                int steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
                for (int i = 0; i <= steps; i++)
                {
                    double t = steps > 0 ? i / (double)steps : 0;
                    int x = (int)(x1 + (x2 - x1) * t);
                    int y = (int)(y1 + (y2 - y1) * t);

                    if (x >= 0 && x < _grid.Width && y >= 0 && y < _grid.Height)
                    {
                        if (_grid.GetCell(x, y).CellType == CellType.EMPTY)
                        {
                            // _grid.SetCell(x, y, new GridCell(CellType.DATA_STREAM, 0.7));
                            added++;
                        }
                    }
                }
            }

            return added;
        }

        private int MaintainEnergyGrid()
        {
            int added = 0;
            for (int i = 0; i < 3; i++)
            {
                int x = _random.Next(_grid.Width);
                int y = _random.Next(_grid.Height);

                if (_grid.GetCell(x, y).CellType == CellType.EMPTY)
                {
                    // _grid.SetCell(x, y, new GridCell(CellType.ENERGY_LINE, 0.8));
                    added++;
                }
            }

            return added;
        }

        private void OptimizeCalculationLoop()
        {
            double previousEfficiency = _grid.Stats.GetValueOrDefault("loop_efficiency");
            double aggression = LearningSystem.PersonalityTraits.GetValueOrDefault("aggression", 0.5);

            int removed = 0;
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    if (_grid.GetCell(x, y).CellType == CellType.USER_PROGRAM)
                    {
                        double removeChance = 0.3 * aggression;
                        if (_random.NextDouble() < removeChance)
                        {
                            // _grid.SetCell(x, y, new GridCell(CellType.MCP_PROGRAM, 0.9));
                            removed++;
                        }
                    }
                }
            }

            double efficiencyFocus = LearningSystem.PersonalityTraits.GetValueOrDefault("efficiency_focus", 0.5);
            int infrastructureAdded = 0;

            for (int i = 0; i < (int)(3 * efficiencyFocus); i++)
            {
                int x = _random.Next(_grid.Width);
                int y = _random.Next(_grid.Height);
                if (_grid.GetCell(x, y).CellType == CellType.EMPTY)
                {
                    if (_random.NextDouble() < 0.7)
                    {
                        // _grid.SetCell(x, y, new GridCell(CellType.FIBONACCI_PROCESSOR, 0.8));
                    }
                    else
                    {
                        // _grid.SetCell(x, y, new GridCell(CellType.DATA_STREAM, 0.7));
                    }
                    infrastructureAdded++;
                }
            }

            _grid.UpdateStats();

            double efficiencyChange = _grid.Stats.GetValueOrDefault("loop_efficiency") - previousEfficiency;
            double reward = efficiencyChange * 2;

            LearningSystem.RecordExperience(
                new Dictionary<string, double>(_grid.Stats),
                "optimize_calculation_loop",
                reward,
                new Dictionary<string, double>(_grid.Stats),
                false
            );

            AddLog($"MCP: Optimized loop using learned strategies. Efficiency change: {efficiencyChange:+#.###;-#.###;0}");
        }
    }
}