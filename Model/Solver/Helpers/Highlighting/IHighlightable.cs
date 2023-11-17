using Global;
using Global.Enums;
using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.LinkGraph;

namespace Model.Solver.Helpers.Highlighting;

public interface IHighlightable
{
    public void HighlightPossibility(int possibility, int row, int col, ChangeColoration coloration);

    public void HighlightPossibility(CellPossibility coord, ChangeColoration coloration)
    {
        HighlightPossibility(coord.Possibility, coord.Row, coord.Col, coloration);
    }

    public void EncirclePossibility(int possibility, int row, int col);

    public void EncirclePossibility(CellPossibility coord)
    {
        EncirclePossibility(coord.Possibility, coord.Row, coord.Col);
    }

    public void HighlightCell(int row, int col, ChangeColoration coloration);

    public void HighlightCell(Cell coord, ChangeColoration coloration)
    {
        HighlightCell(coord.Row, coord.Col, coloration);
    }

    public void EncircleCell(int row, int col);

    public void EncircleCell(Cell coord)
    {
        EncircleCell(coord.Row, coord.Col);
    }

    public void HighlightLinkGraphElement(ILinkGraphElement element, ChangeColoration coloration);

    public void CreateLink(CellPossibility from, CellPossibility to, LinkStrength linkStrength);

    public void CreateLink(ILinkGraphElement from, ILinkGraphElement to, LinkStrength linkStrength);
}
