// TRONGrid.Evolve.cs (partial class)
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public partial class TRONGrid
    {
        public void Evolve()
        {
            // Update animations first
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    cell.UpdateAnimation();

                    // Update temporary visual effects
                    if (cell.Metadata.ContainsKey("energy_spark") && (bool)cell.Metadata["energy_spark"])
                    {
                        int timer = (cell.Metadata.ContainsKey("spark_timer") 
                            ? (int)cell.Metadata["spark_timer"] : 0) - 1;
                        cell.Metadata["spark_timer"] = timer;
                        if (timer <= 0)
                        {
                            cell.Metadata.Remove("energy_spark");
                            cell.Metadata.Remove("spark_timer");
                        }
                    }
                }
            }

            // Perform calculation
            CalculationResult = FibonacciCalculator.CalculateNext();
            LoopIterations++;

            // Update calculation rate
            var calcStats = FibonacciCalculator.GetCalculationStats();
            Stats["calculation_rate"] = (double)calcStats["calculation_rate"];

            var newGrid = new GridCell[Height][];
            for (int y = 0; y < Height; y++)
            {
                newGrid[y] = new GridCell[Width];
                for (int x = 0; x < Width; x++)
                {
                    newGrid[y][x] = new GridCell(CellType.EMPTY, 0.0);
                }
            }

            // Update calculation loop
            if (CalculationLoopActive)
            {
                double loopQuality = Stats["energy_level"] * 0.3 +
                                    Stats["stability"] * 0.4 +
                                    (1.0 - Stats["entropy"]) * 0.3;
                LoopOptimization = 0.7 * LoopOptimization + 0.3 * loopQuality;
            }

            // User program positions for resistance tracking
            var userProgramPositions = new List<(int x, int y, GridCell cell)>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    if (cell.CellType == CellType.USER_PROGRAM)
                    {
                        userProgramPositions.Add((x, y, cell));

                        if (_random.NextDouble() < Stats["user_resistance"])
                        {
                            if (_random.NextDouble() < 0.1)
                            {
                                foreach (var (dx, dy) in _neighborOffsets1)
                                {
                                    int nx = x + dx;
                                    int ny = y + dy;
                                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                                    {
                                        if (_grid[ny][nx].CellType == CellType.EMPTY)
                                        {
                                            newGrid[ny][nx] = new GridCell(CellType.USER_PROGRAM, 0.7);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Track user interference
            int userAdditions = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (newGrid[y][x].CellType == CellType.USER_PROGRAM)
                        userAdditions++;
                }
            }
            UserInterferenceLevel = 0.7 * UserInterferenceLevel + 0.3 * (userAdditions / 10.0);

            // Program movement and interaction
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    bool cellProcessed = false;

                    // USER PROGRAMS
                    if (cell.CellType == CellType.USER_PROGRAM)
                    {
                        if (_random.NextDouble() < Stats["user_resistance"] * 0.3)
                        {
                            var (dx, dy) = _neighborOffsets1[_random.Next(_neighborOffsets1.Length)];
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                            {
                                var targetCell = _grid[ny][nx];
                                if (targetCell.CellType == CellType.EMPTY)
                                {
                                    newGrid[ny][nx] = new GridCell(cell.CellType, cell.Energy * 0.95, cell.Age + 1);
                                    cellProcessed = true;
                                }
                            }
                        }

                        if (!cellProcessed && _random.NextDouble() < 0.3)
                        {
                            int dx = 0, dy = 0;
                            if (_random.NextDouble() < 0.5)
                                dx = _random.Next(2) == 0 ? 1 : -1;
                            else
                                dy = _random.Next(2) == 0 ? 1 : -1;

                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                            {
                                var targetCell = _grid[ny][nx];
                                if (targetCell.CellType == CellType.EMPTY)
                                {
                                    newGrid[ny][nx] = new GridCell(cell.CellType, cell.Energy * 0.95, cell.Age + 1);
                                    newGrid[y][x] = new GridCell(CellType.ENERGY_LINE, cell.Energy * 0.7, 0);
                                    cellProcessed = true;
                                }
                            }
                        }

                        if (!cellProcessed)
                        {
                            newGrid[y][x] = new GridCell(cell.CellType, Math.Max(0.2, cell.Energy - 0.03), cell.Age + 1);
                        }
                    }

                    // MCP PROGRAMS
                    else if (cell.CellType == CellType.MCP_PROGRAM)
                    {
                        if (_random.NextDouble() < 0.4)
                        {
                            var (targetX, targetY) = FindOptimizationTarget(x, y);
                            if (targetX.HasValue)
                            {
                                int dx = targetX > x ? 1 : (targetX < x ? -1 : 0);
                                int dy = targetY > y ? 1 : (targetY < y ? -1 : 0);

                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                                {
                                    var targetCell = _grid[ny][nx];
                                    if (targetCell.CellType == CellType.EMPTY)
                                    {
                                        newGrid[ny][nx] = new GridCell(cell.CellType, Math.Min(1.0, cell.Energy + 0.05), cell.Age + 1);
                                        newGrid[y][x] = new GridCell(CellType.DATA_STREAM, cell.Energy * 0.8, 0);
                                        cellProcessed = true;
                                    }
                                }
                            }
                        }

                        if (!cellProcessed)
                        {
                            newGrid[y][x] = new GridCell(cell.CellType, Math.Max(0.3, cell.Energy - 0.01), cell.Age + 1);
                        }
                    }

                    // SPECIAL PROGRAMS
                    else if (cell.CellType == CellType.SPECIAL_PROGRAM)
                    {
                        var newCell = new GridCell(cell.CellType, Math.Max(0.3, cell.Energy - 0.02), cell.Age + 1, 
                            cell.Stable, cell.SpecialProgramId, new Dictionary<string, object>(cell.Metadata));
                        newGrid[y][x] = newCell;
                    }

                    // GRID BUGS
                    else if (cell.CellType == CellType.GRID_BUG)
                    {
                        if (_random.NextDouble() < 0.4)
                        {
                            var (dx, dy) = _neighborOffsets1[_random.Next(_neighborOffsets1.Length)];
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                            {
                                var target = _grid[ny][nx];
                                if (target.CellType == CellType.USER_PROGRAM || 
                                    target.CellType == CellType.MCP_PROGRAM || 
                                    target.CellType == CellType.SPECIAL_PROGRAM)
                                {
                                    newGrid[ny][nx] = new GridCell(CellType.GRID_BUG, target.Energy * 0.8, 0, false);
                                    newGrid[y][x] = new GridCell(CellType.GRID_BUG, cell.Energy * 0.9, cell.Age + 1, false);
                                    cellProcessed = true;
                                }
                            }
                        }

                        if (!cellProcessed)
                        {
                            newGrid[y][x] = new GridCell(CellType.GRID_BUG, Math.Max(0.1, cell.Energy - 0.05), cell.Age + 1, false);
                        }
                    }

                    // FIBONACCI PROCESSORS
                    else if (cell.CellType == CellType.FIBONACCI_PROCESSOR)
                    {
                        if (cell.Metadata.ContainsKey("temporary") && (bool)cell.Metadata["temporary"])
                        {
                            int lifetime = (cell.Metadata.ContainsKey("lifetime") 
                                ? (int)cell.Metadata["lifetime"] : 0) - 1;
                            
                            if (lifetime <= 0)
                            {
                                if (_random.NextDouble() < 0.3)
                                {
                                    newGrid[y][x] = new GridCell(CellType.ENERGY_LINE, 0.4);
                                }
                                else
                                {
                                    newGrid[y][x] = new GridCell(CellType.EMPTY, 0.0);
                                }
                            }
                            else
                            {
                                var newCell = new GridCell(cell.CellType, Math.Max(0.3, cell.Energy - 0.1), cell.Age + 1);
                                newCell.Metadata = new Dictionary<string, object>(cell.Metadata);
                                newCell.Metadata["lifetime"] = lifetime;
                                newGrid[y][x] = newCell;
                            }
                        }
                        else
                        {
                            var newCell = new GridCell(cell.CellType, Math.Max(0.5, cell.Energy - 0.02), cell.Age + 1);
                            newCell.Metadata = new Dictionary<string, object>(cell.Metadata);
                            newCell.Metadata["age"] = (cell.Metadata.ContainsKey("age") ? (int)cell.Metadata["age"] : 0) + 1;
                            newGrid[y][x] = newCell;
                        }
                    }

                    // INFRASTRUCTURE
                    else if (cell.CellType == CellType.ENERGY_LINE || 
                             cell.CellType == CellType.DATA_STREAM ||
                             cell.CellType == CellType.SYSTEM_CORE || 
                             cell.CellType == CellType.ISO_BLOCK)
                    {
                        var newCell = new GridCell(cell.CellType, cell.Energy, cell.Age + 1);
                        if (cell.CellType == CellType.FIBONACCI_PROCESSOR && 
                            cell.Metadata.ContainsKey("temporary") && (bool)cell.Metadata["temporary"])
                        {
                            int lifetime = (cell.Metadata.ContainsKey("lifetime") 
                                ? (int)cell.Metadata["lifetime"] : 0) - 1;
                            
                            if (lifetime <= 0)
                            {
                                newCell = _random.NextDouble() < 0.5 
                                    ? new GridCell(CellType.ENERGY_LINE, 0.6) 
                                    : new GridCell(CellType.EMPTY, 0.0);
                            }
                            else
                            {
                                newCell.Metadata = new Dictionary<string, object>(cell.Metadata);
                                newCell.Metadata["lifetime"] = lifetime;
                            }
                        }
                        else
                        {
                            newCell.Metadata = new Dictionary<string, object>(cell.Metadata);
                        }
                        newGrid[y][x] = newCell;
                    }

                    // EMPTY cells remain empty in new grid (already initialized)
                }
            }

            // Spawn visual effects
            if (_random.NextDouble() < 0.1)
            {
                SpawnVisualEffect();
            }

            // Update grid
            _grid = newGrid;
            Generation++;

            // Apply special program effects
            foreach (var program in SpecialPrograms.Values)
            {
                if (program.Active)
                {
                    ApplySpecialProgramEffects(program);
                }
            }

            // Update resource history
            _resourceHistory.Enqueue(new Dictionary<string, object>
            {
                ["generation"] = Generation,
                ["loop_efficiency"] = Stats["loop_efficiency"],
                ["energy_balance"] = GetEnergyBalance(),
                ["program_distribution"] = GetProgramDistributionScore(),
                ["user_interference"] = UserInterferenceLevel,
                ["calculation_rate"] = calcStats["calculation_rate"]
            });
            while (_resourceHistory.Count > 50) _resourceHistory.Dequeue();

            UpdateStats();
        }

        private (int? x, int? y) FindOptimizationTarget(int x, int y)
        {
            double bestScore = -1;
            int? bestX = null, bestY = null;

            for (int dy = -3; dy <= 3; dy++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        var cell = _grid[ny][nx];
                        double score = 0;

                        if (cell.CellType == CellType.EMPTY)
                        {
                            int energyNeighbors = CountNeighbors(nx, ny, CellType.ENERGY_LINE);
                            score = energyNeighbors * 0.3;
                        }
                        else if (cell.CellType == CellType.GRID_BUG)
                        {
                            score = -0.5;
                        }
                        else if (cell.CellType == CellType.USER_PROGRAM)
                        {
                            int userNeighbors = CountNeighbors(nx, ny, CellType.USER_PROGRAM);
                            if (userNeighbors > 4)
                                score = -0.3;
                        }

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestX = nx;
                            bestY = ny;
                        }
                    }
                }
            }

            return (bestX, bestY);
        }

        private void SpawnVisualEffect()
        {
            var currentTime = DateTime.Now;
            if ((currentTime - _lastExpensiveOpTime).TotalSeconds < _expensiveOpCooldown)
                return;

            string[] effectTypes = { "energy_pulse", "data_burst", "calculation_spark", "system_pulse" };
            string effectType = effectTypes[_random.Next(effectTypes.Length)];

            int x = _random.Next(Width);
            int y = _random.Next(Height);

            if (effectType == "energy_pulse")
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                        {
                            var cell = _grid[ny][nx];
                            if (cell.CellType == CellType.ENERGY_LINE)
                            {
                                cell.Metadata["energy_pulse"] = true;
                                cell.Metadata["pulse_timer"] = 5;
                            }
                        }
                    }
                }
            }
            else if (effectType == "data_burst")
            {
                for (int i = 0; i < 3; i++)
                {
                    var (dx, dy) = _neighborOffsets1[_random.Next(_neighborOffsets1.Length)];
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        if (_grid[ny][nx].CellType == CellType.DATA_STREAM)
                        {
                            _grid[ny][nx].Metadata["data_burst"] = true;
                            _grid[ny][nx].Metadata["burst_timer"] = 3;
                        }
                    }
                }
            }
            else if (effectType == "calculation_spark")
            {
                if (_grid[y][x].CellType == CellType.EMPTY)
                {
                    _grid[y][x] = new GridCell(CellType.FIBONACCI_PROCESSOR, 0.7);
                    _grid[y][x].Metadata["temporary"] = true;
                    _grid[y][x].Metadata["lifetime"] = 10;
                }
            }

            _visualEffects.Enqueue(new Dictionary<string, object>
            {
                ["type"] = effectType,
                ["x"] = x,
                ["y"] = y,
                ["time"] = currentTime
            });
            while (_visualEffects.Count > 20) _visualEffects.Dequeue();

            _lastExpensiveOpTime = currentTime;
        }

        private double CalculateLoopEfficiency()
        {
            double energyBalance = GetEnergyBalance();
            double programDistribution = GetProgramDistributionScore();

            double cellCooperation = CalculateCellCooperation();
            Stats["cell_cooperation"] = cellCooperation;

            int totalCells = Width * Height;
            int activeCells = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_grid[y][x].CellType != CellType.EMPTY)
                        activeCells++;
                }
            }

            double programRatio = activeCells / (double)totalCells;
            double programOptimality = 1.0 - Math.Abs(programRatio - 0.4) * 2.5;

            double efficiency = energyBalance * 0.3 +
                               programDistribution * 0.25 +
                               programOptimality * 0.2 +
                               cellCooperation * 0.25;

            double calcBonus = Math.Min(0.2, Stats["calculation_rate"] * 0.01);
            efficiency += calcBonus;

            efficiency = Math.Max(0.1, efficiency - (UserInterferenceLevel * 0.3));

            return Math.Min(1.0, efficiency);
        }

        private double CalculateCellCooperation()
        {
            int calculatorCells = 0;
            double totalContribution = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = _grid[y][x];
                    if (cell.CalculationContribution > 0)
                    {
                        calculatorCells++;
                        totalContribution += cell.CalculationContribution;
                    }
                }
            }

            if (calculatorCells == 0) return 0.1;

            double avgContribution = totalContribution / calculatorCells;
            double cellRatio = calculatorCells / (double)(Width * Height);
            double contributionScore = Math.Min(1.0, avgContribution * 10);

            double cooperation = cellRatio * 0.4 +
                                contributionScore * 0.4 +
                                ((double)FibonacciCalculator.GetCalculationStats()["efficiency_score"]) * 0.2;

            return Math.Min(1.0, cooperation);
        }
    }
}