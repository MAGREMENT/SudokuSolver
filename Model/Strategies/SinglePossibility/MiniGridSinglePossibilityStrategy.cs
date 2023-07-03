﻿namespace Model.Strategies.SinglePossibility;

public class MiniGridSinglePossibilityStrategy : ISubStrategy
{
    public bool ApplyOnce(ISolver solver)
    {
        bool wasProgressMade = false;
        
        for (int gridRow = 0; gridRow < 3; gridRow++)
        {
            for (int gridCol = 0; gridCol < 3; gridCol++)
            {
                for (int n = 1; n <= 9; n++)
                {
                    int[]? pos = CheckMiniGridForUnique(solver, gridRow, gridCol, n);
                    if (pos is not null)
                    {
                        solver.AddDefinitiveNumber(n, pos[0], pos[1],
                            new SinglePossibilityLog(n, pos[0], pos[1]));
                        wasProgressMade = true;
                    }
                }
            }
        }
        return wasProgressMade;
    }
    
    private int[]? CheckMiniGridForUnique(ISolver solver, int gridRow, int gridCol, int number)
    {
        int[]? pos = null;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var realRow = gridRow * 3 + row;
                var realCol = gridCol * 3 + col;

                if (solver.Sudoku[realRow, realCol] == number) return null;
                if (solver.Possibilities[realRow, realCol].Peek(number) && solver.Sudoku[realRow, realCol] == 0)
                {
                    if (pos is not null) return null;
                    pos = new[] { realRow, realCol };
                }
            }
        }

        return pos;
    }
}