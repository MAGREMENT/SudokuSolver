﻿namespace Model.Strategies.SinglePossibility;

public class RowSinglePossibilityStrategy : ISolverStrategy
{
    public bool ApplyOnce(ISolver solver)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int n = 1; n <= 9; n++)
            {
                int pos = CheckRowForUnique(solver, row, n);
                if (pos != -1)
                {
                    solver.AddDefinitiveNumber(n, row, pos);
                    return true;
                }
            } 
        }
        
        return false;
    }
    
    private int CheckRowForUnique(ISolver solver, int row, int number)
    {
        int buffer = -1;

        for (int i = 0; i < 9; i++)
        {
            if (solver.Sudoku[row, i] == number) return -1;
            if (solver.Possibilities[row, i].Peek(number) && solver.Sudoku[row, i] == 0)
            {
                if (buffer != -1) return -1;
                buffer = i;
            }
        }

        return buffer;
    }
}