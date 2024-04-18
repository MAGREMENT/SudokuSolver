﻿using Model.Helpers;
using Model.Helpers.Changes;
using Model.Helpers.Highlighting;
using Model.Sudokus.Solver.Utility;
using Model.Sudokus.Solver.Utility.AlmostLockedSets;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Sudokus.Solver;

public interface IStrategyUser : ISudokuSolvingState, IPossibilitiesGiver
{ 
    IReadOnlySudoku Sudoku { get; }
    
    IUpdatableSolvingState StartState { get; }
    
    bool StepsManaged { get; }

    IChangeBuffer<IUpdatableSudokuSolvingState, ISudokuHighlighter> ChangeBuffer { get; }
    
    PreComputer PreComputer { get; }
    
    AlmostHiddenSetSearcher AlmostHiddenSetSearcher { get; }
    
    AlmostNakedSetSearcher AlmostNakedSetSearcher { get; }

    bool UniquenessDependantStrategiesAllowed { get; }

    public ReadOnlyBitSet16 RawPossibilitiesAt(int row, int col);
    
    public ReadOnlyBitSet16 RawPossibilitiesAt(Cell cell) => RawPossibilitiesAt(cell.Row, cell.Column);
}





