﻿using System.Collections.Generic;
using System.Linq;

namespace Model.Strategies.IntersectionRemoval;

public class RowBoxLineReductionStrategy : IStrategy
{
    public string Name { get; } = "Box line reduction";
    
    public StrategyLevel Difficulty { get; } = StrategyLevel.Medium;
    
    public void ApplyOnce(ISolverView solverView)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int number = 1; number <= 9; number++)
            {
                var ppir = solverView.PossibilityPositionsInRow(row, number);
                if (ppir.AreAllInSameMiniGrid())
                {
                    int miniRow = row / 3;
                    int miniCol = ppir.First() / 3;

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int realRow = miniRow * 3 + r;
                            int realCol = miniCol * 3 + c;

                            if (realRow != row && solverView.Sudoku[realRow, realCol] == 0)
                                solverView.RemovePossibility(number, realRow, realCol, this);
                        }
                    }
                }
            }
        }
    }
}