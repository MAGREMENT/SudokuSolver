﻿using System;
using System.Collections.Generic;
using System.Text;
using Model.Core;
using Model.Core.Changes;
using Model.Core.Highlighting;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Sudokus.Solver.Strategies;

public class NakedDoublesStrategy : SudokuStrategy
{
    public const string OfficialName = "Naked Doubles";
    private const InstanceHandling DefaultInstanceHandling = InstanceHandling.UnorderedAll;

    public NakedDoublesStrategy() : base(OfficialName, StepDifficulty.Easy, DefaultInstanceHandling){}

    public override void Apply(ISudokuStrategyUser strategyUser)
    {
        Dictionary<ReadOnlyBitSet16, int> dict = new();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var pos = strategyUser.PossibilitiesAt(row, col);
                if (pos.Count != 2) continue;

                if (dict.TryGetValue(pos, out var otherCol))
                {
                    if (ProcessRow(strategyUser, pos, row, col, otherCol)) return;
                }
                else dict.Add(pos, col);
            }

            dict.Clear();
        }

        for (int col = 0; col < 9; col++)
        {
            for (int row = 0; row < 9; row++)
            {
                var pos = strategyUser.PossibilitiesAt(row, col);
                if (pos.Count != 2) continue;

                if (dict.TryGetValue(pos, out var otherRow))
                {
                    if (ProcessColumn(strategyUser, pos, col, row, otherRow)) return;
                }
                else dict.Add(pos, row);
            }

            dict.Clear();
        }

        for (int miniRow = 0; miniRow < 3; miniRow++)
        {
            for (int miniCol = 0; miniCol < 3; miniCol++)
            {
                int startRow = miniRow * 3;
                int startCol = miniCol * 3;

                for (int gridRow = 0; gridRow < 3; gridRow++)
                {
                    for (int gridCol = 0; gridCol < 3; gridCol++)
                    {
                        int row = startRow + gridRow;
                        int col = startCol + gridCol;

                        var pos = strategyUser.PossibilitiesAt(row, col);
                        if (pos.Count != 2) continue;

                        var gridNumber = gridRow * 3 + gridCol;
                        if (dict.TryGetValue(pos, out var otherGridNumber))
                        {
                            if (ProcessMiniGrid(strategyUser, pos, miniRow, miniCol, gridNumber, otherGridNumber))
                                return;
                        }
                        else dict.Add(pos, gridNumber);
                    }
                }
                
                dict.Clear();
            }
        }
    }

    private bool ProcessRow(ISudokuStrategyUser strategyUser, ReadOnlyBitSet16 possibilities, int row, int col1,
        int col2)
    {
        for (int col = 0; col < 9; col++)
        {
            if (col == col1 || col == col2) continue;

            foreach (var possibility in possibilities.EnumeratePossibilities())
            {
                strategyUser.ChangeBuffer.ProposePossibilityRemoval(possibility, row, col);
            }
        }

        return strategyUser.ChangeBuffer.Commit(
            new LineNakedDoublesReportBuilder(possibilities, row, col1, col2, Unit.Row))
            && StopOnFirstPush;
    }

    private bool ProcessColumn(ISudokuStrategyUser strategyUser, ReadOnlyBitSet16 possibilities, int col,
        int row1, int row2)
    {
        for (int row = 0; row < 9; row++)
        {
            if (row == row1 || row == row2) continue;

            foreach (var possibility in possibilities.EnumeratePossibilities())
            {
                strategyUser.ChangeBuffer.ProposePossibilityRemoval(possibility, row, col);
            }
        }

        return strategyUser.ChangeBuffer.Commit(
            new LineNakedDoublesReportBuilder(possibilities, col, row1, row2, Unit.Column))
            && StopOnFirstPush;
    }

    private bool ProcessMiniGrid(ISudokuStrategyUser strategyUser, ReadOnlyBitSet16 possibilities,
        int miniRow, int miniCol, int gridNumber1, int gridNumber2)
    {
        for (int gridRow = 0; gridRow < 3; gridRow++)
        {
            for (int gridCol = 0; gridCol < 3; gridCol++)
            {
                int gridNumber = gridRow * 3 + gridCol;
                if (gridNumber == gridNumber1 || gridNumber == gridNumber2) continue;

                int row = miniRow * 3 + gridRow;
                int col = miniCol * 3 + gridCol;
                foreach (var possibility in possibilities.EnumeratePossibilities())
                {
                    strategyUser.ChangeBuffer.ProposePossibilityRemoval(possibility, row, col);
                }
            }
        }

        return strategyUser.ChangeBuffer.Commit(
            new MiniGridNakedDoublesReportBuilder(possibilities, miniRow, miniCol, gridNumber1, gridNumber2))
            && StopOnFirstPush;
    }

}

public class LineNakedDoublesReportBuilder : IChangeReportBuilder<IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly ReadOnlyBitSet16 _pos;
    private readonly int _unitNumber;
    private readonly int _other1;
    private readonly int _other2;
    private readonly Unit _unit;

    public LineNakedDoublesReportBuilder(ReadOnlyBitSet16 pos, int unitNumber, int other1, int other2, Unit unit)
    {
        _pos = pos;
        _unitNumber = unitNumber;
        _other1 = other1;
        _other2 = other2;
        _unit = unit;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        var cells = _unit switch
        {
            Unit.Row => new Cell[] { new(_unitNumber, _other1), new(_unitNumber, _other2) },
            Unit.Column => new Cell[] {new(_other1, _unitNumber), new(_other2, _unitNumber)},
            _ => throw new ArgumentOutOfRangeException()
        };
        
        return new ChangeReport<ISudokuHighlighter>(Description(cells), lighter =>
        {
            foreach (var possibility in _pos.EnumeratePossibilities())
            {
                foreach (var cell in cells)
                {
                    lighter.HighlightPossibility(possibility, cell.Row, cell.Column, ChangeColoration.CauseOffOne);
                } 
            }

            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }

    private string Description(IReadOnlyList<Cell> cells)
    {
        var builder = new StringBuilder($"Naked Doubles in {cells[0]}, {cells[1]} for ");

        var n = _pos.NextPossibility(0);
        builder.Append(n + ", " + _pos.NextPossibility(n));

        return builder.ToString();
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}

public class MiniGridNakedDoublesReportBuilder : IChangeReportBuilder<IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly ReadOnlyBitSet16 _pos;
    private readonly int _miniRow;
    private readonly int _miniCol;
    private readonly int _gn1;
    private readonly int _gn2;

    public MiniGridNakedDoublesReportBuilder(ReadOnlyBitSet16 pos, int miniRow, int miniCol, int gn1, int gn2)
    {
        _pos = pos;
        _miniRow = miniRow;
        _miniCol = miniCol;
        _gn1 = gn1;
        _gn2 = gn2;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        List<CellPossibility> cells = new(4);
        
        foreach (var possibility in _pos.EnumeratePossibilities())
        {
            cells.Add(new CellPossibility(_miniRow * 3 + _gn1 / 3, _miniCol * 3 + _gn1 % 3, possibility));
            cells.Add(new CellPossibility(_miniRow * 3 + _gn2 / 3, _miniCol * 3 + _gn2 % 3, possibility));
        }
        
        return new ChangeReport<ISudokuHighlighter>( Explanation(cells), lighter =>
        {
            foreach (var cell in cells)
            {
                lighter.HighlightPossibility(cell, ChangeColoration.CauseOffOne);
            }

            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }
    
    private string Explanation(IReadOnlyList<CellPossibility> cells)
    {
        return $"The cells {cells[0]}, {cells[1]} only contains the possibilities ({_pos}). Any other cell in" +
               $" mini grid {_miniRow * 3 + _miniCol + 1} cannot contain these possibilities";
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}