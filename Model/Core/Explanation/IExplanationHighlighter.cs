using Model.Sudokus.Solver.Utility;
using Model.Utility;

namespace Model.Core.Explanation;

public interface IExplanationHighlighter
{
    void ShowCell(Cell c);
    void ShowCellPossibility(CellPossibility cp);
    void ShowCoverHouse(House ch);
}