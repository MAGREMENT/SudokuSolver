﻿using System.Collections.Generic;

namespace Model.Strategies.SamePossibilities;

public class ColumnSamePossibilitiesStrategy : ISolverStrategy
{
    public bool ApplyOnce(ISolver solver)
    {
        bool wasProgressMade = false;
        
        for (int col = 0; col < 9; col++)
        {
            var listOfPossibilities = GetListOfPossibilities(solver, col);

            if (listOfPossibilities.Count != 0)
            {
                listOfPossibilities.Sort((poss1, poss2) => poss1.Count - poss2.Count);
                
                List<CellPossibilities> currentList = new() { listOfPossibilities[0] };

                for (int i = 1; i < listOfPossibilities.Count; i++)
                {
                    var current = listOfPossibilities[i];

                    if (current.Count != currentList[0].Count || !current.Equals(currentList[0])) currentList.Clear();
                    currentList.Add(current);

                    if (currentList.Count == currentList[0].Count)
                    {
                        wasProgressMade = RemovePossibilitiesFromColumn(solver, col, currentList[0]);
                    }
                }
            }
        }

        return wasProgressMade;
    }

    private List<CellPossibilities> GetListOfPossibilities(ISolver solver, int col)
    {
        List<CellPossibilities> result = new();
        for (int row = 0; row < 9; row++)
        {
            if(solver.Sudoku[row, col] == 0) result.Add(solver.Possibilities[row, col]);
        }

        return result;
    }

    private bool RemovePossibilitiesFromColumn(ISolver solver, int col, CellPossibilities toRemove)
    {
        bool wasProgressMade = false;
        
        for (int row = 0; row < 9; row++)
        {
            if (solver.Sudoku[row, col] == 0 && !solver.Possibilities[row, col].Equals(toRemove))
            {
                CellPossibilities current = solver.Possibilities[row, col];
                foreach (var number in toRemove.GetPossibilities())
                {
                    if (current.Remove(number)) wasProgressMade = true;
                }
            }
        }

        return wasProgressMade;
    }
}