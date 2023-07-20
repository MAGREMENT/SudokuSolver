﻿using System.Collections.Generic;
using Model.Positions;
using Model.Possibilities;

namespace Model;

public interface ISolverView
{
    bool AddDefinitiveNumber(int number, int row, int col, IStrategy strategy);

    bool RemovePossibility(int possibility, int row, int col, IStrategy strategy);

    LinePositions PossibilityPositionsInColumn(int col, int number);

    LinePositions PossibilityPositionsInRow(int row, int number);

    MiniGridPositions PossibilityPositionsInMiniGrid(int miniRow, int miniCol, int number);

    public Sudoku Sudoku { get; }

    public IPossibilities[,] Possibilities { get; }

    public Solver Copy();
}




