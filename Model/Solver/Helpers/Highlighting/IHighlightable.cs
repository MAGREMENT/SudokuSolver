using Global;
using Global.Enums;
using Model.Solver.Position;
using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.Helpers.Highlighting;

public interface IHighlightable
{
    public void HighlightPossibility(int possibility, int row, int col, ChangeColoration coloration);

    public void HighlightPossibility(CellPossibility coord, ChangeColoration coloration)
    {
        HighlightPossibility(coord.Possibility, coord.Row, coord.Column, coloration);
    }

    public void EncirclePossibility(int possibility, int row, int col);

    public void EncirclePossibility(CellPossibility coord)
    {
        EncirclePossibility(coord.Possibility, coord.Row, coord.Column);
    }

    public void HighlightCell(int row, int col, ChangeColoration coloration);

    public void HighlightCell(Cell coord, ChangeColoration coloration)
    {
        HighlightCell(coord.Row, coord.Column, coloration);
    }

    public void EncircleCell(int row, int col);

    public void EncircleCell(Cell coord)
    {
        EncircleCell(coord.Row, coord.Column);
    }
    
    public void EncircleRectangle(CellPossibility from, CellPossibility to, ChangeColoration coloration);

    public void EncircleRectangle(CoverHouse house, ChangeColoration coloration);

    public void HighlightLinkGraphElement(ILinkGraphElement element, ChangeColoration coloration);

    public void CreateLink(CellPossibility from, CellPossibility to, LinkStrength linkStrength);

    public void CreateLink(ILinkGraphElement from, ILinkGraphElement to, LinkStrength linkStrength);
}
