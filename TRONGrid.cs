// TRONGrid.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public partial class TRONGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private GridCell[][] _grid;
        public int Generation { get; private set; }
        public Dictionary<string, SpecialProgram> SpecialPrograms { get; private set; }
        public Dictionary<string, double> Stats { get; private set; }
        public SystemStatus SystemStatus { get; private set; }
        private Queue<Dictionary<string, object>> _history;
        public double OverallEfficiency { get; private set; }
        private Queue<Dictionary<string, object>> _resourceHistory;
        private Queue<Dictionary<string, object>> _visualEffects;
        public bool CalculationLoopActive { get; set; }
        public int LoopIterations { get; private set; }
        public double LoopOptimization { get; private set; }
        public double UserInterferenceLevel { get; private set; }
        private Dictionary<string, int> _userProgramResistance;
        public GridFibonacciCalculator FibonacciCalculator { get; private set; }
        public long CalculationResult { get; set; }
        private int _calcA, _calcB;
        private readonly (int dx, int dy)[] _neighborOffsets1;
        private readonly (int dx, int dy)[] _neighborOffsets8;
        private DateTime _lastExpensiveOpTime;
        private readonly double _expensiveOpCooldown = 0.5;
        private double _lastStability;
        private readonly Random _random = new Random();

        public TRONGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new GridCell[height][];
            for (int y = 0; y < height; y++)
            {
                _grid[y] = new GridCell[width];
                for (int x = 0; x < width; x++)
                {
                    _grid[y][x] = new GridCell(CellType.EMPTY, 0.0);
                }
            }

            Generation = 0;
            SpecialPrograms = new Dictionary<string, SpecialProgram>();
            Stats = new Dictionary<string, double>
            {
                ["user_programs"] = 0,
                ["mcp_programs"] = 0,
                ["grid_bugs"] = 0,
                ["special_programs"] = 0,
                ["energy_level"] = 0.0,
                ["stability"] = 1.0,
                ["entropy"] = 0.1,
                ["loop_efficiency"] = 0.5,
                ["calculation_cycles"] = 0,
                ["resource_usage"] = 0.0,
                ["user_resistance"] = 0.1,
                ["mcp_control"] = 0.5,
                ["optimal_state"] = 0.0,
                ["calculation_rate"] = 0.0,
                ["cell_cooperation"] = 0.5
            };

            SystemStatus = SystemStatus.OPTIMAL;
            _history = new Queue<Dictionary<string, object>>();
            OverallEfficiency = 0.5;
            _resourceHistory = new Queue<Dictionary<string, object>>();
            _visualEffects = new Queue<Dictionary<string, object>>();
            CalculationLoopActive = true;
            LoopIterations = 0;
            LoopOptimization = 0.5;
            UserInterferenceLevel = 0.0;
            _userProgramResistance = new Dictionary<string, int>();
            FibonacciCalculator = new GridFibonacciCalculator(this);
            CalculationResult = 0;
            _calcA = 0;
            _calcB = 1;
            _neighborOffsets1 = new (int, int)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            _neighborOffsets8 = new (int, int)[]
            {
                (-1, -1), (-1, 0), (-1, 1),
                (0, -1),           (0, 1),
                (1, -1),  (1, 0),  (1, 1)
            };
            _lastExpensiveOpTime = DateTime.Now;

            InitializeGrid();
        }

        // return a safe cell even if the coordinates are out of bounds to avoid null references
        public GridCell GetCell(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                return _grid[y][x];
            // provide an empty default cell rather than null
            return new GridCell(CellType.EMPTY, 0.0);
        }

        private List<(int x, int y)> GetNeighbors(int x, int y, int radius = 1)
        {
            var neighbors = new List<(int, int)>();
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        neighbors.Add((nx, ny));
                    }
                }
            }
            return neighbors;
        }

        private double CalculateSystemStability(Dictionary<CellType, int> counts, double totalEnergy, int totalCells)
        {
            if (totalCells == 0) return 1.0;

            double energyLevel = totalEnergy / totalCells;
            double bugRatio = counts[CellType.GRID_BUG] / (double)totalCells;
            double userRatio = counts[CellType.USER_PROGRAM] / (double)totalCells;
            double mcpRatio = counts[CellType.MCP_PROGRAM] / (double)totalCells;

            double energyStability = Math.Min(1.0, energyLevel * 1.5);
            double bugResistance = Math.Max(0.0, 1.0 - (bugRatio * 3.0));

            double totalActive = userRatio + mcpRatio;
            double balance = totalActive > 0 ? 1.0 - Math.Abs(userRatio - mcpRatio) / totalActive : 0.5;

            double infrastructureCells = counts[CellType.ENERGY_LINE] + 
                                        counts[CellType.DATA_STREAM] + 
                                        counts[CellType.SYSTEM_CORE];
            double infrastructureRatio = infrastructureCells / (double)totalCells;
            double infrastructureStability = Math.Min(1.0, infrastructureRatio * 5.0);

            double loopEfficiencyFactor = Stats.GetValueOrDefault("loop_efficiency", 0.5);
            double cellCooperationFactor = Stats.GetValueOrDefault("cell_cooperation", 0.5);

            double stability = energyStability * 0.25 +
                              bugResistance * 0.25 +
                              balance * 0.15 +
                              infrastructureStability * 0.15 +
                              loopEfficiencyFactor * 0.10 +
                              cellCooperationFactor * 0.10;

            double entropyPenalty = Stats.GetValueOrDefault("entropy", 0.1) * 0.3;
            stability = Math.Max(0.0, Math.Min(1.0, stability - entropyPenalty));

            // Smooth transitions
            stability = 0.7 * _lastStability + 0.3 * stability;
            _lastStability = stability;

            return stability;
        }

        public void InitializeGrid()
        {
            // Clear grid
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _grid[y][x] = new GridCell(CellType.EMPTY, 0.0);
                }
            }

            // Create MCP territory with Fibonacci processors
            int mcpStartX = Width / 4;
            int mcpStartY = Height / 2;

            for (int y = Math.Max(0, mcpStartY - 3); y < Math.Min(Height, mcpStartY + 4); y++)
            {
                for (int x = Math.Max(0, mcpStartX - 2); x < Math.Min(Width, mcpStartX + 3); x++)
                {
                    if (_random.NextDouble() < 0.7)
                    {
                        var cell = new GridCell(CellType.MCP_PROGRAM, 0.9);
                        if (_random.NextDouble() < 0.5)
                        {
                            cell.Metadata["is_calculator"] = true;
                            cell.Metadata["calculation_power"] = cell.Energy;
                        }
                        _grid[y][x] = cell;
                    }
                }
            }

            // Create User territory
            int userStartX = 3 * Width / 4;
            int userStartY = Height / 2;

            for (int y = Math.Max(0, userStartY - 3); y < Math.Min(Height, userStartY + 4); y++)
            {
                for (int x = Math.Max(0, userStartX - 2); x < Math.Min(Width, userStartX + 3); x++)
                {
                    if (_random.NextDouble() < 0.6)
                    {
                        _grid[y][x] = new GridCell(CellType.USER_PROGRAM, 0.8);
                    }
                }
            }

            // Add grid bugs
            for (int i = 0; i < 8; i++)
            {
                int x = Width / 2 + _random.Next(-8, 9);
                int y = Height / 2 + _random.Next(-8, 9);
                x = Math.Max(0, Math.Min(Width - 1, x));
                y = Math.Max(0, Math.Min(Height - 1, y));
                
                if (_grid[y][x].CellType == CellType.EMPTY)
                {
                    _grid[y][x] = new GridCell(CellType.GRID_BUG, 0.6) { Stable = false };
                }
            }

            // System core
            int coreX = Width / 2;
            int coreY = Height / 2;
            var coreCell = new GridCell(CellType.SYSTEM_CORE, 1.0);
            coreCell.Metadata["core_pulse"] = true;
            _grid[coreY][coreX] = coreCell;

            // Energy grid lines
            for (int i = 0; i < Width; i += 3)
            {
                var cell = new GridCell(CellType.ENERGY_LINE, 0.8);
                cell.AnimationFrame = i % 4;
                if (coreY < Height)
                {
                    _grid[coreY][i] = cell;
                }
            }

            // Data streams
            for (int i = 0; i < Height; i += 3)
            {
                var cell = new GridCell(CellType.DATA_STREAM, 0.7);
                cell.AnimationFrame = i % 4;
                if (coreX < Width)
                {
                    _grid[i][coreX] = cell;
                }
            }

            // Fibonacci processors
            for (int i = 0; i < 5; i++)
            {
                int x = _random.Next(Math.Max(0, coreX - 15), Math.Min(Width - 1, coreX + 15));
                int y = _random.Next(Math.Max(0, coreY - 10), Math.Min(Height - 1, coreY + 10));
                if (_grid[y][x].CellType == CellType.EMPTY)
                {
                    var cell = new GridCell(CellType.FIBONACCI_PROCESSOR, 0.8);
                    cell.Metadata["calculation_power"] = 1.0;
                    _grid[y][x] = cell;
                }
            }

            // Fill empty cells
            var emptyCells = new List<(int, int)>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_grid[y][x].CellType == CellType.EMPTY)
                    {
                        emptyCells.Add((x, y));
                    }
                }
            }

            int fillCount = (int)(emptyCells.Count * 0.2);
            for (int i = 0; i < fillCount; i++)
            {
                if (emptyCells.Count > 0)
                {
                    int idx = _random.Next(emptyCells.Count);
                    var (x, y) = emptyCells[idx];
                    var cellType = _random.Next(4) switch
                    {
                        0 => CellType.USER_PROGRAM,
                        1 => CellType.MCP_PROGRAM,
                        2 => CellType.ENERGY_LINE,
                        _ => CellType.DATA_STREAM
                    };
                    double energy = _random.NextDouble() * 0.4 + 0.5; // 0.5 to 0.9
                    _grid[y][x] = new GridCell(cellType, energy);
                    emptyCells.RemoveAt(idx);
                }
            }

            UpdateStats();
        }
    }
}