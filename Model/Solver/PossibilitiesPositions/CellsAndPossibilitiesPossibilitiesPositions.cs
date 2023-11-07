using System.Collections.Generic;
using Model.Solver.Positions;
using Model.Solver.Possibilities;
using Model.Solver.StrategiesUtil;

namespace Model.Solver.PossibilitiesPositions;

public class CellsAndPossibilitiesPossibilitiesPositions : IPossibilitiesPositions
{
    private readonly Cell[] _cells;
    private readonly IPossibilitiesHolder _snapshot;
    private GridPositions? _gp;

    public CellsAndPossibilitiesPossibilitiesPositions(Cell[] cells, IPossibilities possibilities, IPossibilitiesHolder snapshot)
    {
        _cells = cells;
        Possibilities = possibilities;
        _snapshot = snapshot;
    }
    
    
    public IEnumerable<int> EachPossibility()
    {
        return Possibilities;
    }

    public IEnumerable<Cell> EachCell()
    {
        return _cells;
    }

    public IEnumerable<Cell> EachCellWithPossibility(int possibility)
    {
        foreach (var cell in _cells)
        {
            if (_snapshot.PossibilitiesAt(cell.Row, cell.Col).Peek(possibility)) yield return cell;
        }
    }

    public IEnumerable<int> EachPossibilityWithCell(Cell cell)
    {
        foreach (var possibility in Possibilities)
        {
            if (_snapshot.PossibilitiesAt(cell.Row, cell.Col).Peek(possibility)) yield return possibility;
        }
    }

    public IPossibilities Possibilities { get; }

    public GridPositions Positions
    {
        get
        {
            if (_gp is null)
            {
                _gp = new GridPositions();
                foreach (var cell in _cells)
                {
                    _gp.Add(cell);
                }
            }

            return _gp;
        }
    }

    public int PossibilityCount => Possibilities.Count;
    public int PositionsCount => _cells.Length;
}