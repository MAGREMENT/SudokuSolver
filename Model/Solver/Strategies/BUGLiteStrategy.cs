﻿using System;
using System.Collections.Generic;
using Global;
using Global.Enums;
using Model.Solver.Helpers.Changes;
using Model.Solver.Position;
using Model.Solver.Possibility;
using Model.Solver.StrategiesUtility;

namespace Model.Solver.Strategies;

public class BUGLiteStrategy : AbstractStrategy //TODO improve detection (problem with structureDone)
{
    public const string OfficialName = "BUG-Lite";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;

    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;

    private readonly int _maxStructSize;
    
    public BUGLiteStrategy(int maxStructSize) : base(OfficialName, StrategyDifficulty.Hard, DefaultBehavior)
    {
        _maxStructSize = maxStructSize;
        UniquenessDependency = UniquenessDependency.FullyDependent;
    }
    
    public override void Apply(IStrategyManager strategyManager)
    {
        var structuresDone = new HashSet<GridPositions>();
        
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var poss = strategyManager.PossibilitiesAt(row, col);
                if (poss.Count != 2) continue;

                var first = new Cell(row, col);
                var startR = row / 3 * 3;
                var startC = col / 3 * 3;

                for (int r = row % 3; r < 3; r++)
                {
                    var row2 = startR + r;
                    
                    for (int c = col % 3; c < 3; c++)
                    {
                        var col2 = startC + c;
                        if ((row2 == row && col2 == col) || !strategyManager.PossibilitiesAt(row2, col2).Equals(poss)) continue;

                        var second = new Cell(row2, col2);
                        var bcp = new BiCellPossibilities(first, second, poss);
                        var conditionsToMeet = new List<IBUGLiteCondition>();
                        if (row != row2)
                        {
                            foreach (var p in poss)
                            {
                                conditionsToMeet.Add(new RowBUGLiteCondition(first, second, p));
                            }
                        }

                        if (col != col2)
                        {
                            foreach (var p in poss)
                            {
                                conditionsToMeet.Add(new ColumnBUGLiteCondition(first, second, p));
                            }
                        }
                        
                        if (Search(strategyManager, new HashSet<BiCellPossibilities> {bcp},
                            new GridPositions {first, second}, conditionsToMeet,
                            new HashSet<IBUGLiteCondition>(), structuresDone)) return;
                    }
                }
            }
        }
    }

    private bool Search(IStrategyManager strategyManager, HashSet<BiCellPossibilities> bcp, GridPositions structure, 
        List<IBUGLiteCondition> conditionsToMeet, HashSet<IBUGLiteCondition> conditionsMet, HashSet<GridPositions> structuresDone)
    {
        var current = conditionsToMeet[0];
        conditionsToMeet.RemoveAt(0);
        conditionsMet.Add(current);

        foreach (var match in current.ConditionMatches(strategyManager, structure))
        {
            bool ok = true;
            foreach (var otherCondition in match.OtherConditions)
            {
                if (conditionsMet.Contains(otherCondition))
                {
                    ok = false;
                    break;
                }
            }

            if (!ok) continue;
            
            structure.Add(match.BiCellPossibilities.One);
            structure.Add(match.BiCellPossibilities.Two);
            if (structuresDone.Contains(structure))
            {
                structure.Remove(match.BiCellPossibilities.One);
                structure.Remove(match.BiCellPossibilities.Two);
                continue;
            }

            structuresDone.Add(structure.Copy());

            List<IBUGLiteCondition> met = new();
            foreach (var otherCondition in match.OtherConditions)
            {
                var i = conditionsToMeet.IndexOf(otherCondition);
                if (i == -1) conditionsToMeet.Add(otherCondition);
                else
                {
                    conditionsToMeet.RemoveAt(i);
                    conditionsMet.Add(otherCondition);
                    met.Add(otherCondition);
                }
            }

            bcp.Add(match.BiCellPossibilities);

            if (conditionsToMeet.Count == 0)
            {
                if (Process(strategyManager, bcp)) return true;
            }
            else if (structure.Count < _maxStructSize &&
                      Search(strategyManager, bcp, structure, conditionsToMeet, conditionsMet, structuresDone)) return true;
            
            structure.Remove(match.BiCellPossibilities.One);
            structure.Remove(match.BiCellPossibilities.Two);
            bcp.Remove(match.BiCellPossibilities);
            var count = match.OtherConditions.Length - met.Count;
            conditionsToMeet.RemoveRange(conditionsToMeet.Count - count, count);
            foreach (var c in met)
            {
                conditionsMet.Remove(c);
                conditionsToMeet.Add(c);
            }
        }

        conditionsToMeet.Insert(0, current);
        conditionsMet.Remove(current);
        
        return false;
    }

    private bool Process(IStrategyManager strategyManager, HashSet<BiCellPossibilities> bcp)
    {
        var cellsNotInStructure = new List<Cell>();
        var possibilitiesNotInStructure = Possibilities.NewEmpty();

        foreach (var b in bcp)
        {
            var no1 = strategyManager.PossibilitiesAt(b.One).Difference(b.Possibilities);
            if (no1.Count > 0)
            {
                cellsNotInStructure.Add(b.One);
                foreach (var p in no1)
                {
                    possibilitiesNotInStructure.Add(p);
                }
            }

            var no2 = strategyManager.PossibilitiesAt(b.Two).Difference(b.Possibilities);
            if (no2.Count > 0)
            {
                cellsNotInStructure.Add(b.Two);
                foreach (var p in no2)
                {
                    possibilitiesNotInStructure.Add(p);
                }
            }
        }

        if (cellsNotInStructure.Count == 1)
        {
            var c = cellsNotInStructure[0];
            foreach (var p in FindStructurePossibilitiesFor(c, bcp))
            {
                strategyManager.ChangeBuffer.ProposePossibilityRemoval(p, c);
            }
        }
        else if (cellsNotInStructure.Count == 2)
        {
            var p1 = FindStructurePossibilitiesFor(cellsNotInStructure[0], bcp);
            var p2 = FindStructurePossibilitiesFor(cellsNotInStructure[1], bcp);
            if(p1.Equals(p2))
            {
                var asArray = p1.ToArray();
                for (int i = 0; i < 2; i++)
                {
                    var cp1 = new CellPossibility(cellsNotInStructure[0], asArray[i]);
                    var cp2 = new CellPossibility(cellsNotInStructure[1], asArray[i]);
                    if (Cells.AreStronglyLinked(strategyManager, cp1, cp2))
                    {
                        var other = asArray[(i + 1) % 2];
                        strategyManager.ChangeBuffer.ProposePossibilityRemoval(other, cellsNotInStructure[0]);
                        strategyManager.ChangeBuffer.ProposePossibilityRemoval(other, cellsNotInStructure[1]);
                    }
                }
            }
        }

        if (possibilitiesNotInStructure.Count == 1)
        {
            var p = possibilitiesNotInStructure.First();
            foreach (var ssc in Cells.SharedSeenCells(cellsNotInStructure))
            {
                strategyManager.ChangeBuffer.ProposePossibilityRemoval(p, ssc);
            }
        }

        return strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this,
            new BUGLiteReportBuilder(bcp)) && OnCommitBehavior == OnCommitBehavior.Return;
    }

    private static IReadOnlyPossibilities FindStructurePossibilitiesFor(Cell cell, IEnumerable<BiCellPossibilities> bcp)
    {
        foreach (var b in bcp)
        {
            if (b.One == cell || b.Two == cell) return b.Possibilities;
        }

        return Possibilities.NewEmpty();
    }
}

public record BiCellPossibilities(Cell One, Cell Two, IReadOnlyPossibilities Possibilities);

public record BUGLiteConditionMatch(BiCellPossibilities BiCellPossibilities, params IBUGLiteCondition[] OtherConditions);

public interface IBUGLiteCondition
{ 
    IEnumerable<BUGLiteConditionMatch> ConditionMatches(IStrategyManager strategyManager, GridPositions done);
}

public class RowBUGLiteCondition : IBUGLiteCondition
{
    private readonly Cell _one;
    private readonly Cell _two;
    private readonly int _possibility;

    public RowBUGLiteCondition(Cell one, Cell two, int possibility)
    {
        _one = one;
        _two = two;
        _possibility = possibility;
    }

    public IEnumerable<BUGLiteConditionMatch> ConditionMatches(IStrategyManager strategyManager, GridPositions done)
    {
        var miniCol = _one.Column / 3;

        for (int c = 0; c < 3; c++)
        {
            if (c == miniCol) continue;

            for (int i = 0; i < 3; i++)
            {
                var first = new Cell(_one.Row, c * 3 + i);
                if (done.Peek(first) || strategyManager.Sudoku[first.Row, first.Column] != 0) continue;

                for (int j = 0; j < 3; j++)
                {
                    var second = new Cell(_two.Row, c * 3 + j);
                    if (done.Peek(first) || strategyManager.Sudoku[second.Row, second.Column] != 0) continue;

                    var and = strategyManager.PossibilitiesAt(first).And(strategyManager.PossibilitiesAt(second));
                    if (and.Count < 2 || !and.Peek(_possibility)) continue;

                    foreach (var p in and)
                    {
                        if (p == _possibility) continue;

                        var poss = Possibilities.NewEmpty();
                        poss.Add(_possibility);
                        poss.Add(p);
                        var bcp = new BiCellPossibilities(first, second, poss);

                        if (first.Column == second.Column) yield return new BUGLiteConditionMatch(
                            bcp, new RowBUGLiteCondition(first, second, p));
                        else yield return new BUGLiteConditionMatch(bcp, new RowBUGLiteCondition(
                                first, second, p), new ColumnBUGLiteCondition(first, second, p),
                            new ColumnBUGLiteCondition(first, second, _possibility));
                    }
                }
            }
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is RowBUGLiteCondition rblc && rblc._possibility == _possibility &&
               rblc._one.Row == _one.Row && rblc._two.Row == _two.Row;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_possibility, _one.Row, _two.Row);
    }

    public override string ToString()
    {
        return $"{_possibility}r{_one.Row}r{_two.Row}";
    }
}

public class ColumnBUGLiteCondition : IBUGLiteCondition
{
    private readonly Cell _one;
    private readonly Cell _two;
    private readonly int _possibility;

    public ColumnBUGLiteCondition(Cell one, Cell two, int possibility)
    {
        _one = one;
        _two = two;
        _possibility = possibility;
    }

    public IEnumerable<BUGLiteConditionMatch> ConditionMatches(IStrategyManager strategyManager, GridPositions done)
    {
        var miniRow = _one.Row / 3;

        for (int r = 0; r < 3; r++)
        {
            if (r == miniRow) continue;

            for (int i = 0; i < 3; i++)
            {
                var first = new Cell(r * 3 + i, _one.Column);
                if (done.Peek(first) || strategyManager.Sudoku[first.Row, first.Column] != 0) continue;

                for (int j = 0; j < 3; j++)
                {
                    var second = new Cell(r * 3 + j, _two.Column);
                    if (done.Peek(first) || strategyManager.Sudoku[second.Row, second.Column] != 0) continue;

                    var and = strategyManager.PossibilitiesAt(first).And(strategyManager.PossibilitiesAt(second));
                    if (and.Count < 2 || !and.Peek(_possibility)) continue;

                    foreach (var p in and)
                    {
                        if (p == _possibility) continue;

                        var poss = Possibilities.NewEmpty();
                        poss.Add(_possibility);
                        poss.Add(p);
                        var bcp = new BiCellPossibilities(first, second, poss);

                        if (first.Row == second.Row)
                            yield return new BUGLiteConditionMatch(
                                bcp, new ColumnBUGLiteCondition(first, second, p));
                        else
                            yield return new BUGLiteConditionMatch(bcp, new ColumnBUGLiteCondition(
                                    first, second, p), new RowBUGLiteCondition(first, second, p),
                                new RowBUGLiteCondition(first, second, _possibility));
                    }
                }
            }
        }
    }
    
    public override bool Equals(object? obj)
    {
        return obj is ColumnBUGLiteCondition cblc && cblc._possibility == _possibility &&
               cblc._one.Column == _one.Column && cblc._two.Column == _two.Column;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_possibility, _one.Column, _two.Column);
    }

    public override string ToString()
    {
        return $"{_possibility}c{_one.Column}c{_two.Column}";
    }
}

public class BUGLiteReportBuilder : IChangeReportBuilder
{
    private readonly IEnumerable<BiCellPossibilities> _bcp;

    public BUGLiteReportBuilder(IEnumerable<BiCellPossibilities> bcp)
    {
        _bcp = bcp;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", lighter =>
        {
            foreach (var b in _bcp)
            {
                foreach (var p in b.Possibilities)
                {
                    lighter.HighlightPossibility(p, b.One.Row, b.One.Column, ChangeColoration.CauseOffTwo);
                    lighter.HighlightPossibility(p, b.Two.Row, b.Two.Column, ChangeColoration.CauseOffTwo);
                }
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }
}