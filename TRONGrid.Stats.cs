// TRONGrid.Stats.cs (partial class)
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public partial class TRONGrid
    {
        public void UpdateStats()
        {
            var counts = new Dictionary<CellType, int>();
            foreach (CellType type in Enum.GetValues(typeof(CellType)))
            {
                counts[type] = 0;
            }

            double totalEnergy = 0;
            int totalCells = Width * Height;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    counts[cell.CellType]++;
                    totalEnergy += cell.Energy;
                }
            }

            int specialProgramCount = SpecialPrograms.Count;
            double bugRatio = counts[CellType.GRID_BUG] / (double)totalCells;

            int activeCells = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_grid[y][x].CellType != CellType.EMPTY)
                        activeCells++;
                }
            }
            double resourceUsage = activeCells / (double)totalCells;

            double userResistance = Math.Min(0.5, counts[CellType.USER_PROGRAM] / (double)totalCells * 2);
            double mcpControl = Math.Min(1.0, counts[CellType.MCP_PROGRAM] / (counts[CellType.USER_PROGRAM] + 0.1));

            var calcStats = FibonacciCalculator.GetCalculationStats();
            double cellCooperation = CalculateCellCooperation();

            Stats["user_programs"] = counts[CellType.USER_PROGRAM];
            Stats["mcp_programs"] = counts[CellType.MCP_PROGRAM];
            Stats["grid_bugs"] = counts[CellType.GRID_BUG];
            Stats["special_programs"] = specialProgramCount;
            Stats["energy_level"] = totalEnergy / totalCells;
            Stats["stability"] = CalculateSystemStability(counts, totalEnergy, totalCells);
            Stats["entropy"] = bugRatio * 2;
            Stats["loop_efficiency"] = CalculateLoopEfficiency();
            Stats["calculation_cycles"] = LoopIterations;
            Stats["resource_usage"] = resourceUsage;
            Stats["user_resistance"] = userResistance;
            Stats["mcp_control"] = mcpControl;
            Stats["optimal_state"] = CalculateOptimalState();
            Stats["calculation_rate"] = (double)calcStats["calculation_rate"];
            Stats["cell_cooperation"] = cellCooperation;

            double stability = Stats["stability"];
            if (stability > 0.75)
                SystemStatus = SystemStatus.OPTIMAL;
            else if (stability > 0.6)
                SystemStatus = SystemStatus.STABLE;
            else if (stability > 0.4)
                SystemStatus = SystemStatus.DEGRADED;
            else if (stability > 0.2)
                SystemStatus = SystemStatus.CRITICAL;
            else
                SystemStatus = SystemStatus.COLLAPSE;

            _history.Enqueue(new Dictionary<string, object>
            {
                ["generation"] = Generation,
                ["user_programs"] = Stats["user_programs"],
                ["mcp_programs"] = Stats["mcp_programs"],
                ["grid_bugs"] = Stats["grid_bugs"],
                ["stability"] = Stats["stability"],
                ["special_programs"] = Stats["special_programs"],
                ["loop_efficiency"] = Stats["loop_efficiency"]
            });
            while (_history.Count > 100) _history.Dequeue();
        }

        public int GetCalculatorCount()
        {
            int count = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    if (cell.CellType == CellType.MCP_PROGRAM &&
                        cell.Metadata.ContainsKey("is_calculator") &&
                        (bool)cell.Metadata["is_calculator"])
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public bool QuarantineBug(int x, int y)
        {
            bool success = false;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        if (_grid[ny][nx].CellType == CellType.GRID_BUG)
                        {
                            _grid[ny][nx] = new GridCell(CellType.ISO_BLOCK, 0.9);
                            success = true;
                        }
                    }
                }
            }
            if (success) UpdateStats();
            return success;
        }

        public (string? programId, string message) AddSpecialProgram(string programType, string name, int x, int y, string creator = "USER")
        {
            const int maxSpecialPrograms = 10;
            if (SpecialPrograms.Count >= maxSpecialPrograms)
                return (null, "Maximum number of special programs reached");

            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return (null, $"Coordinates ({x},{y}) are out of bounds");

            if (_grid[y][x].CellType != CellType.EMPTY)
                return (null, "Cell is not empty");

            string programId = $"SP_{SpecialPrograms.Count:0000}";

            var program = new SpecialProgram(programId, name, programType, x, y, creator);
            SpecialPrograms[programId] = program;

            var cell = new GridCell(CellType.SPECIAL_PROGRAM, program.Energy, 0, true, programId,
                new Dictionary<string, object> { ["name"] = name, ["type"] = programType });
            _grid[y][x] = cell;

            UpdateStats();
            return (programId, $"Special program '{name}' created at ({x},{y})");
        }

        public (bool success, string message) ExecuteSpecialProgramFunction(string programId, string functionName, int? targetX = null, int? targetY = null)
        {
            if (!SpecialPrograms.ContainsKey(programId))
                return (false, "Program not found");

            var program = SpecialPrograms[programId];
            var (success, message) = program.ExecuteFunction(functionName, _grid, targetX, targetY);

            if (!program.Active)
            {
                int x = program.X, y = program.Y;
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    if (_grid[y][x].SpecialProgramId == programId)
                    {
                        _grid[y][x] = new GridCell(CellType.EMPTY, 0.0);
                    }
                }
            }

            UpdateStats();
            return (success, message);
        }

        public string GetGridString()
        {
            string gridStr = "";
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    gridStr += _grid[y][x].GetChar();
                }
                gridStr += "\n";
            }
            return gridStr;
        }

        private double GetEnergyBalance()
        {
            int energySources = 0;
            int energyUsers = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    if (cell.CellType == CellType.ENERGY_LINE || cell.CellType == CellType.SYSTEM_CORE)
                        energySources++;
                    if (cell.CellType == CellType.USER_PROGRAM || 
                        cell.CellType == CellType.MCP_PROGRAM || 
                        cell.CellType == CellType.SPECIAL_PROGRAM)
                        energyUsers++;
                }
            }

            if (energyUsers == 0) return 1.0;
            return Math.Min(1.0, energySources / (double)Math.Max(1, energyUsers));
        }

        private double GetProgramDistributionScore()
        {
            int isolated = 0;
            int clustered = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    if (cell.CellType == CellType.USER_PROGRAM || cell.CellType == CellType.MCP_PROGRAM)
                    {
                        int neighbors = CountNeighbors(x, y);
                        if (neighbors <= 1)
                            isolated++;
                        else if (neighbors >= 3)
                            clustered++;
                    }
                }
            }

            double totalPrograms = Stats["user_programs"] + Stats["mcp_programs"];
            if (totalPrograms == 0) return 0.5;

            double isolationRatio = isolated / totalPrograms;
            double clusteringRatio = clustered / totalPrograms;

            return 1.0 - Math.Abs(isolationRatio - clusteringRatio);
        }

        private double CalculateOptimalState()
        {
            double loopEfficiency = Stats["loop_efficiency"];
            double userResistance = Stats["user_resistance"];
            double mcpControl = Stats["mcp_control"];
            double resourceUsage = Stats["resource_usage"];

            return loopEfficiency * 0.4 +
                   (1.0 - userResistance) * 0.3 +
                   mcpControl * 0.2 +
                   (1.0 - resourceUsage) * 0.1;
        }

        public int CountNeighbors(int x, int y, CellType? cellType = null)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        if (cellType == null)
                        {
                            if (_grid[ny][nx].CellType != CellType.EMPTY)
                                count++;
                        }
                        else
                        {
                            if (_grid[ny][nx].CellType == cellType)
                                count++;
                        }
                    }
                }
            }
            return count;
        }

        public int CountUserNeighbors(int x, int y)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        if (_grid[ny][nx].CellType == CellType.USER_PROGRAM)
                            count++;
                    }
                }
            }
            return count;
        }

        public bool AddProgram(int x, int y, CellType cellType, double energy = 0.8)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                var cell = new GridCell(cellType, energy);
                if (cellType == CellType.MCP_PROGRAM && _random.NextDouble() < 0.4)
                {
                    cell.Metadata["is_calculator"] = true;
                    cell.Metadata["calculation_power"] = energy;
                }
                _grid[y][x] = cell;
                UpdateStats();
                return true;
            }
            return false;
        }

        private void ApplySpecialProgramEffects(SpecialProgram program)
        {
            // Implementation for special program effects
            // This would contain the logic for special program behaviors
        }
    }
}