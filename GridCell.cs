// GridCell.cs
using System;
using System.Collections.Generic;

namespace GridSimulation
{
    public class GridCell
    {
        public CellType CellType { get; set; }
        public double Energy { get; set; }
        public int Age { get; set; }
        public bool Stable { get; set; }
        public string? SpecialProgramId { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public int AnimationFrame { get; set; }
        public bool Processing { get; set; }
        public double CalculationContribution { get; set; }
        private static readonly Random _random = new Random();

        public GridCell(CellType cellType, double energy)
        {
            CellType = cellType;
            Energy = energy;
            Age = 0;
            Stable = true;
            SpecialProgramId = null;
            Metadata = new Dictionary<string, object>();
            AnimationFrame = 0;
            Processing = false;
            CalculationContribution = 0.0;

            // Initialize animation for certain cell types
            if (cellType == CellType.DATA_STREAM || 
                cellType == CellType.ENERGY_LINE || 
                cellType == CellType.FIBONACCI_PROCESSOR)
            {
                AnimationFrame = _random.Next(0, 4);
            }
        }

        // additional constructors to support extended usages in partial class methods
        public GridCell(CellType cellType, double energy, int age)
            : this(cellType, energy)
        {
            Age = age;
        }

        public GridCell(CellType cellType, double energy, int age, bool stable)
            : this(cellType, energy, age)
        {
            Stable = stable;
        }

        public GridCell(CellType cellType, double energy, int age, bool stable, string? specialProgramId, Dictionary<string, object> metadata)
            : this(cellType, energy, age, stable)
        {
            SpecialProgramId = specialProgramId;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public char GetChar()
        {
            // Single-character representations only to maintain grid alignment
            if (CellType == CellType.DATA_STREAM)
            {
                char[] streamChars = { '~', '~', '≈', '≈' };
                return streamChars[AnimationFrame % 4];
            }
            else if (CellType == CellType.ENERGY_LINE)
            {
                char[] energyChars = { '=', '=', '≡', '≡' };
                return energyChars[AnimationFrame % 4];
            }
            else if (CellType == CellType.FIBONACCI_PROCESSOR)
            {
                if (Processing)
                {
                    char[] processorChars = { '◉', '◎', '●', '○', '◌', '⊕', '⊗', '∅' };
                    return processorChars[AnimationFrame % 8];
                }
                else
                {
                    char[] processorChars = { 'F', 'φ', 'f', 'Φ' };
                    return processorChars[AnimationFrame % 4];
                }
            }
            else if (Processing)
            {
                return AnimationFrame % 2 == 0 ? '○' : '●';
            }

            Dictionary<CellType, char> chars = new Dictionary<CellType, char>
            {
                { CellType.EMPTY, ' ' },
                { CellType.USER_PROGRAM, 'U' },
                { CellType.MCP_PROGRAM, 'M' },
                { CellType.GRID_BUG, 'B' },
                { CellType.ISO_BLOCK, '#' },
                { CellType.ENERGY_LINE, '=' },
                { CellType.DATA_STREAM, '~' },
                { CellType.SYSTEM_CORE, '@' },
                { CellType.SPECIAL_PROGRAM, 'S' },
                { CellType.FIBONACCI_PROCESSOR, 'F' }
            };

            return chars.ContainsKey(CellType) ? chars[CellType] : '?';
        }

        public int GetColor()
        {
            Dictionary<CellType, int> colors = new Dictionary<CellType, int>
            {
                { CellType.EMPTY, 0 },
                { CellType.USER_PROGRAM, 1 },
                { CellType.MCP_PROGRAM, 2 },
                { CellType.GRID_BUG, 3 },
                { CellType.ISO_BLOCK, 4 },
                { CellType.ENERGY_LINE, 5 },
                { CellType.DATA_STREAM, 6 },
                { CellType.SYSTEM_CORE, 7 },
                { CellType.SPECIAL_PROGRAM, 8 },
                { CellType.FIBONACCI_PROCESSOR, 9 }
            };

            return colors.ContainsKey(CellType) ? colors[CellType] : 0;
        }

        public void UpdateAnimation()
        {
            if (CellType == CellType.DATA_STREAM || 
                CellType == CellType.ENERGY_LINE || 
                CellType == CellType.FIBONACCI_PROCESSOR)
            {
                AnimationFrame++;
                
                // Visual feedback for processing
                if (CalculationContribution > 0)
                {
                    Processing = true;
                    AnimationFrame += 2; // Faster animation when processing
                }
                else
                {
                    Processing = false;
                }
            }
        }
    }
}