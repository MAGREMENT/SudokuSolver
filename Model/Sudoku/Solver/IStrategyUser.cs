﻿using Model.Helpers;
using Model.Helpers.Changes;
using Model.Sudoku.Solver.StrategiesUtility;
using Model.Sudoku.Solver.StrategiesUtility.AlmostLockedSets;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Sudoku.Solver;

public interface IStrategyUser : ISudokuSolvingState, IPossibilitiesGiver
{ 
    IReadOnlySudoku Sudoku { get; }
    
    IUpdatableSolvingState StartState { get; }
    
    bool LogsManaged { get; }

    IChangeBuffer<IUpdatableSudokuSolvingState> ChangeBuffer { get; }
    
    PreComputer PreComputer { get; }
    
    AlmostHiddenSetSearcher AlmostHiddenSetSearcher { get; }
    
    AlmostNakedSetSearcher AlmostNakedSetSearcher { get; }

    bool UniquenessDependantStrategiesAllowed { get; }

    public ReadOnlyBitSet16 RawPossibilitiesAt(int row, int col);
    
    public ReadOnlyBitSet16 RawPossibilitiesAt(Cell cell) => RawPossibilitiesAt(cell.Row, cell.Column);
}





