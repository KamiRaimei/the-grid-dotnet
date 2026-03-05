// GridFibonacciCalculator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GridSimulation
{
    public class GridFibonacciCalculator
    {
        private readonly TRONGrid _grid;
        private List<long> _fibSequence;
        private double _calculationAccumulator;
        private DateTime _lastCalculationTime;
        private double _calculationRate;
        private double _totalContributions;
        private Dictionary<string, double> _cellContributions;
        private Queue<Dictionary<string, object>> _contributionHistory;
        private double _efficiencyScore;
        private double _optimizationLevel;
        private readonly object _lockObject = new object();
        private readonly Random _random = new Random();

        public GridFibonacciCalculator(TRONGrid grid)
        {
            _grid = grid;
            _fibSequence = new List<long> { 0, 1 };
            _calculationAccumulator = 0.0;
            _lastCalculationTime = DateTime.Now;
            _calculationRate = 0.0;
            _totalContributions = 0;
            _cellContributions = new Dictionary<string, double>();
            _contributionHistory = new Queue<Dictionary<string, object>>();
            _efficiencyScore = 0.5;
            _optimizationLevel = 0.0;
        }

        public long CalculateNext()
        {
            var contributions = new List<(int x, int y, double contribution)>();
            double totalEnergy = 0;
            int activeCalculators = 0;

            // Reset processing flags
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);
                    cell.CalculationContribution = 0.0;
                    cell.Processing = false;
                }
            }

            // Collect contributions from all cells
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var cell = _grid.GetCell(x, y);
                    double contribution = 0.0;

                    if (cell.CellType == CellType.MCP_PROGRAM)
                    {
                        contribution = cell.Energy * 0.3;
                        if (cell.Metadata.ContainsKey("is_calculator") && 
                            (bool)cell.Metadata["is_calculator"])
                        {
                            contribution *= 2.0;
                            activeCalculators++;
                        }
                    }
                    else if (cell.CellType == CellType.FIBONACCI_PROCESSOR)
                    {
                        contribution = cell.Energy * 0.8;
                        if (!cell.Metadata.ContainsKey("temporary") || 
                            !(bool)cell.Metadata["temporary"])
                        {
                            contribution *= 1.5;
                        }
                        activeCalculators++;
                    }
                    else if (cell.CellType == CellType.USER_PROGRAM)
                    {
                        contribution = cell.Energy * 0.1;
                    }
                    else if (cell.CellType == CellType.DATA_STREAM)
                    {
                        contribution = cell.Energy * 0.15;
                    }
                    else if (cell.CellType == CellType.ENERGY_LINE)
                    {
                        contribution = cell.Energy * 0.1;
                    }
                    else if (cell.CellType == CellType.SYSTEM_CORE)
                    {
                        contribution = cell.Energy * 0.25;
                    }

                    // Apply efficiency bonus
                    double systemEfficiency = _grid.Stats["loop_efficiency"];
                    contribution *= (0.5 + systemEfficiency * 0.5);

                    // Apply bug penalty
                    double bugPenalty = CalculateBugPenalty(x, y);
                    contribution *= (1.0 - bugPenalty);

                    // Apply collaboration bonus
                    if (contribution > 0)
                    {
                        double collaborationBonus = CalculateCollaborationBonus(x, y);
                        contribution *= (1.0 + collaborationBonus);
                    }

                    if (contribution > 0)
                    {
                        contributions.Add((x, y, contribution));
                        totalEnergy += contribution;
                        cell.CalculationContribution = contribution;
                        cell.Processing = true;
                    }
                }
            }

            // Calculate rate
            DateTime currentTime = DateTime.Now;
            double timeDelta = (currentTime - _lastCalculationTime).TotalSeconds;
            if (timeDelta > 0)
            {
                _calculationRate = totalEnergy / timeDelta;
            }
            _lastCalculationTime = currentTime;

            // Visual feedback
            foreach (var (x, y, contribution) in contributions)
            {
                var cell = _grid.GetCell(x, y);
                if (contribution > 0 && cell.CellType == CellType.FIBONACCI_PROCESSOR)
                {
                    cell.CalculationContribution = contribution;
                    cell.Processing = true;

                    if (contribution > 0.3)
                    {
                        cell.Metadata["pulse_strength"] = Math.Min(1.0, contribution);
                        cell.Metadata["pulse_timer"] = 3;
                    }
                }
            }

            // Update accumulator
            _calculationAccumulator += totalEnergy;
            _totalContributions = totalEnergy;

            // Store history
            var historyEntry = new Dictionary<string, object>
            {
                ["time"] = currentTime,
                ["total_energy"] = totalEnergy,
                ["active_calculators"] = activeCalculators,
                ["efficiency"] = _grid.Stats["loop_efficiency"]
            };
            _contributionHistory.Enqueue(historyEntry);
            while (_contributionHistory.Count > 100)
                _contributionHistory.Dequeue();

            // Process Fibonacci sequence
            const double calculationThreshold = 1000;
            const int maxStepsPerFrame = 10;
            int stepsTaken = 0;

            while (_calculationAccumulator >= calculationThreshold && stepsTaken < maxStepsPerFrame)
            {
                long nextFib = _fibSequence[_fibSequence.Count - 2] + _fibSequence[_fibSequence.Count - 1];
                _fibSequence.Add(nextFib);

                if (_fibSequence.Count > 100)
                {
                    _fibSequence = _fibSequence.Skip(_fibSequence.Count - 100).ToList();
                }

                _calculationAccumulator -= calculationThreshold;
                stepsTaken++;

                if (stepsTaken == 1)
                {
                    CreateCalculationVisualFeedback(contributions);
                }
            }

            // Update efficiency
            UpdateEfficiencyScore(contributions, activeCalculators);

            return _fibSequence.Last();
        }

        private double CalculateBugPenalty(int x, int y)
        {
            double penalty = 0.0;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                    {
                        if (_grid.GetCell(nx, ny).CellType == CellType.GRID_BUG)
                        {
                            double distance = Math.Sqrt(dx * dx + dy * dy);
                            penalty += 0.1 / Math.Max(1.0, distance);
                        }
                    }
                }
            }
            return Math.Min(0.5, penalty);
        }

        private double CalculateCollaborationBonus(int x, int y)
        {
            double bonus = 0.0;
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
                        if (neighbor.CellType == CellType.MCP_PROGRAM &&
                            neighbor.Metadata.ContainsKey("is_calculator") &&
                            (bool)neighbor.Metadata["is_calculator"])
                        {
                            calculatorNeighbors++;
                        }
                        else if (neighbor.CellType == CellType.FIBONACCI_PROCESSOR)
                        {
                            calculatorNeighbors += 2;
                        }
                    }
                }
            }

            bonus = calculatorNeighbors * 0.05;
            return Math.Min(0.3, bonus);
        }

        private void CreateCalculationVisualFeedback(List<(int x, int y, double contribution)> contributions)
        {
            if (contributions.Count == 0) return;

            var topContributors = contributions
                .OrderByDescending(c => c.contribution)
                .Take(5)
                .ToList();

            foreach (var (x, y, contribution) in topContributors)
            {
                var cell = _grid.GetCell(x, y);
                cell.Metadata["last_major_contribution"] = DateTime.Now;
                cell.Metadata["contribution_streak"] = 
                    (cell.Metadata.ContainsKey("contribution_streak") 
                        ? (int)cell.Metadata["contribution_streak"] : 0) + 1;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < _grid.Width && ny >= 0 && ny < _grid.Height)
                        {
                            var neighbor = _grid.GetCell(nx, ny);
                            if (neighbor.CellType == CellType.EMPTY && _random.NextDouble() < 0.3)
                            {
                                neighbor.Metadata["energy_spark"] = true;
                                neighbor.Metadata["spark_timer"] = 3;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateEfficiencyScore(List<(int x, int y, double contribution)> contributions, int activeCalculators)
        {
            if (contributions.Count == 0)
            {
                _efficiencyScore *= 0.95;
                return;
            }

            double totalContributors = contributions.Count;
            double avgContribution = contributions.Average(c => c.contribution);

            double calculatorFactor = Math.Min(1.0, activeCalculators / 10.0);
            double systemFactor = _grid.Stats["loop_efficiency"];
            double energyFactor = 1.0 - _grid.Stats["entropy"] * 0.5;

            double newEfficiency = calculatorFactor * 0.4 + systemFactor * 0.3 + energyFactor * 0.3;
            _efficiencyScore = 0.8 * _efficiencyScore + 0.2 * newEfficiency;

            if (_contributionHistory.Count > 1)
            {
                var history = _contributionHistory.ToList();
                double recentGain = (double)history[history.Count - 1]["total_energy"] - 
                                   (double)history[history.Count - 2]["total_energy"];
                
                if (recentGain > 0)
                {
                    _optimizationLevel = Math.Min(1.0, _optimizationLevel + 0.05);
                }
                else
                {
                    _optimizationLevel = Math.Max(0.0, _optimizationLevel - 0.1);
                }
            }
        }

        public string FormatFibonacciNumber(long num)
        {
            string numStr = num.ToString();
            if (numStr.Length > 30)
            {
                return numStr.Substring(0, 30) + $"... ({numStr.Length} digits)";
            }
            return numStr;
        }

        public Dictionary<string, object> GetCalculationStats()
        {
            int activeCalculators = 0;
            if (_contributionHistory.Count > 0)
            {
                var last = _contributionHistory.Last();
                activeCalculators = (int)last["active_calculators"];
            }

            string formattedFib = FormatFibonacciNumber(_fibSequence.Last());

            return new Dictionary<string, object>
            {
                ["current_fibonacci"] = _fibSequence.Last(),
                ["current_fibonacci_formatted"] = formattedFib,
                ["calculation_rate"] = _calculationRate,
                ["accumulator"] = _calculationAccumulator,
                ["efficiency_score"] = _efficiencyScore,
                ["optimization_level"] = _optimizationLevel,
                ["sequence_length"] = _fibSequence.Count,
                ["total_contributions"] = _totalContributions,
                ["active_calculators"] = activeCalculators
            };
        }
    }
}