﻿using Model.Helpers.Changes;
using Model.Sudoku.Solver.StrategiesUtility;
using Model.Sudoku.Solver.StrategiesUtility.Graphs;
using Model.Utility;

namespace Model.Helpers.Highlighting;

public interface ISolvingStateHighlighter
{
    public void HighlightPossibility(int possibility, int row, int col, ChangeColoration coloration);

    public void HighlightPossibility(CellPossibility coord, ChangeColoration coloration)
    {
        HighlightPossibility(coord.Possibility, coord.Row, coord.Column, coloration);
    }

    public void HighlightPossibility(Cell cell, int possibility, ChangeColoration coloration)
    {
        HighlightPossibility(possibility, cell.Row, cell.Column, coloration);
    }
    
    public void HighlightCell(int row, int col, ChangeColoration coloration);

    public void HighlightCell(Cell coord, ChangeColoration coloration)
    {
        HighlightCell(coord.Row, coord.Column, coloration);
    }
    
    public void CreateLink(CellPossibility from, CellPossibility to, LinkStrength linkStrength);
}

public interface ISudokuHighlighter : ISolvingStateHighlighter
{
    public void EncirclePossibility(int possibility, int row, int col);

    public void EncirclePossibility(CellPossibility coord)
    {
        EncirclePossibility(coord.Possibility, coord.Row, coord.Column);
    }

    public void EncircleCell(int row, int col);

    public void EncircleCell(Cell coord)
    {
        EncircleCell(coord.Row, coord.Column);
    }
    
    public void EncircleRectangle(CellPossibility from, CellPossibility to, ChangeColoration coloration);

    public void EncircleRectangle(CoverHouse house, ChangeColoration coloration);

    public void HighlightLinkGraphElement(ISudokuElement element, ChangeColoration coloration);

    public void CreateLink(ISudokuElement from, ISudokuElement to, LinkStrength linkStrength);
}

public interface ITectonicHighlighter : ISolvingStateHighlighter
{
    
}