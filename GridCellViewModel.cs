// GridCellViewModel.cs
using Avalonia.Media;

namespace GridSimulation
{
    public class GridCellViewModel
    {
        private readonly GridCell _cell;

        public GridCellViewModel(GridCell cell)
        {
            _cell = cell;
        }

        public string Char => _cell.GetChar().ToString();
        
        public IBrush Background
        {
            get
            {
                return _cell.CellType switch
                {
                    CellType.EMPTY => new SolidColorBrush(Color.FromRgb(40, 40, 50)),
                    CellType.USER_PROGRAM => new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                    CellType.MCP_PROGRAM => new SolidColorBrush(Color.FromRgb(232, 17, 35)),
                    CellType.GRID_BUG => new SolidColorBrush(Color.FromRgb(40, 180, 40)),
                    CellType.ISO_BLOCK => new SolidColorBrush(Colors.White),
                    CellType.ENERGY_LINE => new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                    CellType.DATA_STREAM => new SolidColorBrush(Color.FromRgb(0, 200, 200)),
                    CellType.SYSTEM_CORE => new SolidColorBrush(Color.FromRgb(200, 0, 200)),
                    CellType.SPECIAL_PROGRAM => new SolidColorBrush(Color.FromRgb(0, 255, 255)),
                    CellType.FIBONACCI_PROCESSOR => new SolidColorBrush(Color.FromRgb(255, 255, 100)),
                    _ => new SolidColorBrush(Color.FromRgb(40, 40, 50))
                };
            }
        }
    }
}