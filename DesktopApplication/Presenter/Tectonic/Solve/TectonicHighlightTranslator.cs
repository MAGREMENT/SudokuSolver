﻿using Model;
using Model.Helpers.Changes;
using Model.Helpers.Highlighting;
using Model.Sudoku.Solver.StrategiesUtility;
using Model.Sudoku.Solver.StrategiesUtility.Graphs;

namespace DesktopApplication.Presenter.Tectonic.Solve;

public class TectonicHighlightTranslator : ITectonicHighlighter
{
    private readonly ITectonicDrawer _drawer;

    public TectonicHighlightTranslator(ITectonicDrawer drawer)
    {
        _drawer = drawer;
    }

    public void Translate(IHighlightable<ITectonicHighlighter> highlightable)
    {
        highlightable.Highlight(this);
        _drawer.Refresh();
    }

    public void HighlightPossibility(int possibility, int row, int col, ChangeColoration coloration)
    {
        _drawer.FillPossibility(row, col, possibility, coloration);
    }

    public void HighlightCell(int row, int col, ChangeColoration coloration)
    {
        _drawer.FillCell(row, col, coloration);
    }

    public void CreateLink(CellPossibility from, CellPossibility to, LinkStrength linkStrength)
    {
        _drawer.CreateLink(from.Row, from.Column, from.Possibility, to.Row,
            to.Column, to.Possibility, linkStrength, LinkOffsetSidePriority.Any);
    }
}