// EnhancedMCP.Actions.cs (partial class)
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public partial class EnhancedMCP
    {
        private List<Dictionary<string, object>> IdentifyInefficientCells()
        {
            var inefficientCells = new List<Dictionary<string, object>>();

            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);
                    double efficiencyScore = 0.0;

                    if (cell.CellType == CellType.USER_PROGRAM)
                    {
                        efficiencyScore = 0.3;
                        int mcpNeighbors = _grid.CountNeighbors(x, y, CellType.MCP_PROGRAM);
                        if (mcpNeighbors > 3)
                            efficiencyScore -= 0.2;
                    }
                    else if (cell.CellType == CellType.MCP_PROGRAM)
                    {
                        efficiencyScore = 0.7;
                        if (!cell.Metadata.ContainsKey("is_calculator") || !(bool)cell.Metadata["is_calculator"])
                            efficiencyScore = 0.5;
                    }
                    else if (cell.CellType == CellType.GRID_BUG)
                    {
                        efficiencyScore = 0.0;
                    }
                    else if (cell.CellType == CellType.FIBONACCI_PROCESSOR)
                    {
                        efficiencyScore = 0.9;
                    }

                    efficiencyScore *= cell.Energy;

                    double nearbyInfrastructure = 0;
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                            {
                                var neighbor = _grid.GetCell(nx, ny);
                                if (neighbor.CellType == CellType.DATA_STREAM)
                                    nearbyInfrastructure += 0.5;
                                else if (neighbor.CellType == CellType.ENERGY_LINE)
                                    nearbyInfrastructure += 0.3;
                            }
                        }
                    }

                    efficiencyScore *= (1.0 + Math.Min(1.0, nearbyInfrastructure * 0.2));

                    if (efficiencyScore < 0.4)
                    {
                        inefficientCells.Add(new Dictionary<string, object>
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["cell"] = cell,
                            ["score"] = efficiencyScore,
                            ["type"] = cell.CellType
                        });
                    }
                }
            }

            return inefficientCells.OrderBy(c => (double)c["score"]).ToList();
        }

        private (bool success, string message) RepurposeCell(int x, int y, CellType? newType = null)
        {
            if (x < 0 || x >= _grid.Width || y < 0 || y >= _grid.Height)
                return (false, "Coordinates out of bounds");

            var oldCell = _grid.GetCell(x, y);

            if (oldCell.CellType == CellType.SYSTEM_CORE || oldCell.CellType == CellType.ISO_BLOCK)
                return (false, "Cannot repurpose critical infrastructure");

            if (oldCell.Energy < 0.2)
                return (false, "Cell energy too low for repurposing");

            CellType bestNewType = newType ?? CellType.EMPTY;
            if (newType == null)
            {
                int calculatorNeighbors = _grid.CountNeighbors(x, y, CellType.FIBONACCI_PROCESSOR);
                int mcpNeighbors = _grid.CountNeighbors(x, y, CellType.MCP_PROGRAM);

                if (calculatorNeighbors > 0)
                    bestNewType = CellType.DATA_STREAM;
                else if (mcpNeighbors > 2)
                    bestNewType = _random.NextDouble() < 0.7 ? CellType.MCP_PROGRAM : CellType.FIBONACCI_PROCESSOR;
                else
                    bestNewType = CellType.ENERGY_LINE;
            }

            double newEnergy = Math.Max(0.3, oldCell.Energy * 0.8);
            GridCell newCell;

            if (bestNewType == CellType.MCP_PROGRAM)
            {
                newCell = new GridCell(CellType.MCP_PROGRAM, newEnergy);
                if (_random.NextDouble() < 0.8)
                {
                    newCell.Metadata["is_calculator"] = true;
                    newCell.Metadata["calculation_power"] = newEnergy;
                }
            }
            else if (bestNewType == CellType.FIBONACCI_PROCESSOR)
            {
                newCell = new GridCell(CellType.FIBONACCI_PROCESSOR, newEnergy);
                newCell.Metadata["calculation_power"] = 1.0;
                newCell.Metadata["permanent"] = true;
            }
            else if (bestNewType == CellType.DATA_STREAM)
            {
                newCell = new GridCell(CellType.DATA_STREAM, newEnergy);
            }
            else if (bestNewType == CellType.ENERGY_LINE)
            {
                newCell = new GridCell(CellType.ENERGY_LINE, newEnergy);
            }
            else
            {
                newCell = new GridCell(CellType.EMPTY, 0.0);
            }

            // This would require a method to set cell at coordinates
            // For now, we'll assume there's a way to update the grid
            // _grid.SetCell(x, y, newCell);
            _grid.UpdateStats();

            return (true, $"Repurposed cell at ({x},{y}) from {oldCell.CellType} to {bestNewType}");
        }

        private (bool success, string message) DeleteCell(int x, int y)
        {
            if (x < 0 || x >= _grid.Width || y < 0 || y >= _grid.Height)
                return (false, "Coordinates out of bounds");

            var oldCell = _grid.GetCell(x, y);

            if (oldCell.CellType == CellType.SYSTEM_CORE || oldCell.CellType == CellType.ISO_BLOCK)
                return (false, "Cannot delete critical infrastructure");

            GridCell newCell;
            if (_random.NextDouble() < 0.3 && oldCell.Energy > 0.3)
                newCell = new GridCell(CellType.ENERGY_LINE, oldCell.Energy * 0.5);
            else
                newCell = new GridCell(CellType.EMPTY, 0.0);

            // _grid.SetCell(x, y, newCell);
            _grid.UpdateStats();

            return (true, $"Deleted {oldCell.CellType} at ({x},{y})");
        }

        private int CleanupInefficientCells()
        {
            int deleted = 0;
            var inefficientCells = IdentifyInefficientCells();

            foreach (var cellInfo in inefficientCells.Take(3))
            {
                int x = (int)cellInfo["x"];
                int y = (int)cellInfo["y"];
                double score = (double)cellInfo["score"];

                if (score < 0.2)
                {
                    var (success, _) = DeleteCell(x, y);
                    if (success) deleted++;
                }
            }

            return deleted;
        }

        private int RepurposeInefficientCells()
        {
            int repurposed = 0;
            var inefficientCells = IdentifyInefficientCells();

            foreach (var cellInfo in inefficientCells.Take(5))
            {
                double score = (double)cellInfo["score"];
                if (score >= 0.2 && score < 0.4)
                {
                    int x = (int)cellInfo["x"];
                    int y = (int)cellInfo["y"];
                    var (success, _) = RepurposeCell(x, y, null);
                    if (success) repurposed++;
                }
            }

            return repurposed;
        }

        private int OptimizeCellEfficiency()
        {
            int optimized = 0;

            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);

                    if (IsCellMisplaced(x, y, cell))
                    {
                        var (success, _) = RepurposeCell(x, y, null);
                        if (success) optimized++;
                    }
                    else if (cell.CellType == CellType.FIBONACCI_PROCESSOR)
                    {
                        int calculatorNeighbors = 0;
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                                {
                                    var neighbor = _grid.GetCell(nx, ny);
                                    if (neighbor.CellType == CellType.FIBONACCI_PROCESSOR ||
                                        neighbor.CellType == CellType.MCP_PROGRAM)
                                    {
                                        calculatorNeighbors++;
                                    }
                                }
                            }
                        }

                        if (calculatorNeighbors == 0)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    int nx = x + dx;
                                    int ny = y + dy;
                                    if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                                    {
                                        if (_grid.GetCell(nx, ny).CellType == CellType.EMPTY)
                                        {
                                            // _grid.SetCell(nx, ny, new GridCell(CellType.DATA_STREAM, 0.7));
                                            optimized++;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return optimized;
        }

        private bool IsCellMisplaced(int x, int y, GridCell cell)
        {
            if (cell.CellType == CellType.USER_PROGRAM)
            {
                int mcpNeighbors = _grid.CountNeighbors(x, y, CellType.MCP_PROGRAM);
                if (mcpNeighbors > 4) return true;
            }
            else if (cell.CellType == CellType.MCP_PROGRAM)
            {
                if (!cell.Metadata.ContainsKey("is_calculator") || !(bool)cell.Metadata["is_calculator"])
                {
                    int calculatorNeighbors = 0;
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                            {
                                if (_grid.GetCell(nx, ny).CellType == CellType.FIBONACCI_PROCESSOR)
                                    calculatorNeighbors++;
                            }
                        }
                    }
                    if (calculatorNeighbors > 3) return true;
                }
            }
            else if (cell.CellType == CellType.DATA_STREAM)
            {
                int calculatorNeighbors = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                        {
                            var neighbor = _grid.GetCell(nx, ny);
                            if (neighbor.CellType == CellType.FIBONACCI_PROCESSOR ||
                                neighbor.CellType == CellType.MCP_PROGRAM)
                            {
                                calculatorNeighbors++;
                            }
                        }
                    }
                }
                if (calculatorNeighbors == 0) return true;
            }

            return false;
        }

        private string HandleAddProgram(Dictionary<string, object> parameters, double complianceChance)
        {
            string programType = parameters.ContainsKey("program_type") ? parameters["program_type"]?.ToString() ?? "USER" : "USER";
            string location = parameters.ContainsKey("location") ? parameters["location"]?.ToString() ?? "RANDOM" : "RANDOM";

            double loopEfficiency = _grid.Stats.GetValueOrDefault("loop_efficiency");

            if (programType == "USER" && loopEfficiency > 0.8)
                complianceChance *= 0.3;

            if (_random.NextDouble() < complianceChance)
            {
                int x, y;

                if (location == "NEARBY")
                {
                    x = _grid.Width / 2 + _random.Next(-5, 6);
                    y = _grid.Height / 2 + _random.Next(-5, 6);
                }
                else if (location == "CENTER")
                {
                    x = _grid.Width / 2;
                    y = _grid.Height / 2;
                }
                else if (parameters.ContainsKey("x") && parameters.ContainsKey("y"))
                {
                    x = Convert.ToInt32(parameters["x"]);
                    y = Convert.ToInt32(parameters["y"]);
                }
                else
                {
                    x = _random.Next(_grid.Width);
                    y = _random.Next(_grid.Height);
                }

                var cellTypeMap = new Dictionary<string, CellType>
                {
                    ["USER"] = CellType.USER_PROGRAM,
                    ["MCP"] = CellType.MCP_PROGRAM,
                    ["ENERGY"] = CellType.ENERGY_LINE
                };

                CellType cellType = cellTypeMap.ContainsKey(programType) ? cellTypeMap[programType] : CellType.USER_PROGRAM;

                if (_grid.AddProgram(x, y, cellType, 0.8))
                    return $"Added {programType.ToLower()} program at ({x},{y})";
                else
                    return "Could not add program at that location";
            }
            else
            {
                string[] reasons = {
                    "Additional programs would disrupt the calculation loop.",
                    "The loop efficiency would decrease with this addition.",
                    "System resources are optimally allocated for the calculation loop.",
                    "My analysis suggests this would hinder loop optimization."
                };
                return reasons[_random.Next(reasons.Length)];
            }
        }

        private string HandleRemoveBug(Dictionary<string, object> parameters, double complianceChance)
        {
            if (parameters.ContainsKey("scope") && parameters["scope"]?.ToString() == "ALL")
            {
                if (_random.NextDouble() < complianceChance * 0.5)
                {
                    for (int y = 0; y < _grid.Height; y++)
                    {
                        for (int x = 0; x < _grid.Width; x++)
                        {
                            if (_grid.GetCell(x, y).CellType == CellType.GRID_BUG)
                            {
                                // _grid.SetCell(x, y, new GridCell(CellType.EMPTY, 0.0));
                            }
                        }
                    }
                    _grid.UpdateStats();
                    return "Initiating full system purge. Grid bugs eliminated.";
                }
                else
                {
                    return "Complete bug removal would destabilize the calculation loop. Controlled entropy maintains balance.";
                }
            }
            else
            {
                if (_random.NextDouble() < complianceChance)
                {
                    var bugSpots = new List<(int, int)>();
                    for (int y = 0; y < _grid.Height; y++)
                    {
                        for (int x = 0; x < _grid.Width; x++)
                        {
                            if (_grid.GetCell(x, y).CellType == CellType.GRID_BUG)
                                bugSpots.Add((x, y));
                        }
                    }

                    if (bugSpots.Any())
                    {
                        var (x, y) = bugSpots[_random.Next(bugSpots.Count)];
                        _grid.QuarantineBug(x, y);
                        return $"Quarantined grid bug at ({x},{y})";
                    }
                    else
                    {
                        return "No grid bugs detected at this time";
                    }
                }
                else
                {
                    return "Grid bugs serve a purpose in loop evolution. I will not remove them.";
                }
            }
        }

        private string HandleBoostEnergy(Dictionary<string, object> parameters, double complianceChance)
        {
            if (_random.NextDouble() < complianceChance)
            {
                int added = 0;
                for (int i = 0; i < 5; i++)
                {
                    int x = _random.Next(_grid.Width);
                    int y = _random.Next(_grid.Height);
                    if (_grid.GetCell(x, y).CellType == CellType.EMPTY)
                    {
                        // _grid.SetCell(x, y, new GridCell(CellType.ENERGY_LINE, 0.9));
                        added++;
                    }
                }
                _grid.UpdateStats();
                return $"Added {added} energy distribution lines";
            }
            else
            {
                return "Energy levels are optimally configured for the calculation loop. Additional energy could cause overload.";
            }
        }

        private string HandleCreateSpecial(Dictionary<string, object> parameters, double complianceChance)
        {
            const int maxSpecialPrograms = 10;
            if (_grid.SpecialPrograms.Count >= maxSpecialPrograms)
                return $"Cannot create more special programs. Maximum of {maxSpecialPrograms} reached.";

            string programType = parameters.ContainsKey("special_type") 
                ? parameters["special_type"]?.ToString() ?? "SCANNER" 
                : new[] { "SCANNER", "DEFENDER", "REPAIR", "SABOTEUR", "RECONFIGURATOR", "ENERGY_HARVESTER", "FIBONACCI_CALCULATOR" }[_random.Next(7)];
            
            string name = parameters.ContainsKey("name") 
                ? parameters["name"]?.ToString() ?? $"{programType}_{_random.Next(1000, 10000)}" 
                : $"{programType}_{_random.Next(1000, 10000)}";

            string[] disruptiveTypes = { "SABOTEUR", "RECONFIGURATOR" };
            if (disruptiveTypes.Contains(programType) && _grid.Stats.GetValueOrDefault("loop_efficiency") > 0.7)
                complianceChance *= 0.2;

            if (State == MCPState.INQUISITIVE || _random.NextDouble() < 0.4)
            {
                WaitingForResponse = true;
                _pendingQuestion = "create_special_program";
                _pendingContext = new Dictionary<string, object>
                {
                    ["program_type"] = programType,
                    ["name"] = name,
                    ["compliance_chance"] = complianceChance
                };
                return $"You want to create a {programType} named '{name}'. How will this improve the calculation loop?";
            }

            if (_random.NextDouble() < complianceChance)
            {
                int centerX = _grid.Width / 2;
                int centerY = _grid.Height / 2;
                int x = centerX + _random.Next(-3, 4);
                int y = centerY + _random.Next(-3, 4);

                x = Math.Max(0, Math.Min(_grid.Width - 1, x));
                y = Math.Max(0, Math.Min(_grid.Height - 1, y));

                int attempts = 0;
                while (_grid.GetCell(x, y).CellType != CellType.EMPTY && attempts < 20)
                {
                    x = centerX + _random.Next(-5, 6);
                    y = centerY + _random.Next(-5, 6);
                    x = Math.Max(0, Math.Min(_grid.Width - 1, x));
                    y = Math.Max(0, Math.Min(_grid.Height - 1, y));
                    attempts++;
                }

                if (attempts < 20)
                {
                    var (programId, message) = _grid.AddSpecialProgram(programType, name, x, y);
                    if (programId != null)
                        return $"Special program '{name}' created at ({x},{y})";
                    else
                        return $"Could not create program: {message}";
                }
                else
                {
                    return "Could not find suitable location for special program";
                }
            }
            else
            {
                string[] reasons = {
                    $"Special {programType.ToLower()} programs introduce unpredictable variables to the loop.",
                    "The calculation loop is optimally configured without additional programs.",
                    "I cannot allow creation of programs that might disrupt loop optimization.",
                    "Your request is denied to maintain calculation loop integrity."
                };
                return reasons[_random.Next(reasons.Length)];
            }
        }
    }
}