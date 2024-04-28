﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Model.Helpers;
using Model.Helpers.Changes;
using Model.Helpers.Highlighting;
using Model.Sudokus.Solver.Position;
using Model.Sudokus.Solver.Utility;
using Model.Sudokus.Solver.Utility.Graphs;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Sudokus.Solver.Strategies;

public class ThorsHammerStrategy : SudokuStrategy
{
    public const string OfficialName = "Thor's Hammer";
    private const InstanceHandling DefaultInstanceHandling = InstanceHandling.UnorderedAll;

    private readonly IBoxLoopFinder _finder;
    
    public ThorsHammerStrategy(IBoxLoopFinder finder) : base(OfficialName, StrategyDifficulty.Extreme, DefaultInstanceHandling)
    {
        _finder = finder;
    }

    
    public override void Apply(ISudokuStrategyUser strategyUser)
    {
        Dictionary<int, MiniGridPositions> boxCandidates = new();
        foreach (var combination in CombinationCalculator.EveryCombinationWithSpecificCount(3, CombinationCalculator.NumbersSample))
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var pos1 = strategyUser.MiniGridPositionsAt(r, c, combination[0]);
                    if (pos1.Count == 0) continue;
                    
                    var pos2 = strategyUser.MiniGridPositionsAt(r, c, combination[1]);
                    if (pos2.Count == 0) continue;

                    var pos3 = strategyUser.MiniGridPositionsAt(r, c, combination[2]);
                    if (pos3.Count == 0) continue;

                    var total = pos1.Or(pos2).Or(pos3);
                    if (total.Count < 3) continue;

                    if (total.AtLeastOneInEachRows() && total.AtLeastOnInEachColumns()) boxCandidates.Add(r * 3 + c, total);
                }
            }

            if (boxCandidates.Count >= 4 && TryEveryLoop(strategyUser, combination, boxCandidates)) return;
            
            boxCandidates.Clear();
        }
    }

    private bool TryEveryLoop(ISudokuStrategyUser strategyUser, int[] possibilities,
        Dictionary<int, MiniGridPositions> boxCandidates)
    {
        var graph = new BoxGraph();
        foreach (var n in boxCandidates.Keys)
        {
            foreach (var k in boxCandidates.Keys)
            {
                if (n == k) continue;
                if (n / 3 == k / 3 || n % 3 == k % 3) graph.AddLink(n, k);
            }
        }

        foreach (var loop in _finder.FindLoops(graph))
        {
            if (TryEveryPattern(strategyUser, possibilities, loop, boxCandidates,
                    new Dictionary<int, MiniGridPositions>(), 0)) return true;
        }

        return false;
    }
    
    private bool TryEveryPattern(ISudokuStrategyUser strategyUser, int[] possibilities, BoxLoop loop,
        Dictionary<int, MiniGridPositions> boxCandidates, Dictionary<int, MiniGridPositions> current, int n)
    {
        if (n == loop.Length) return Search(strategyUser, possibilities, loop, current);

        foreach (var mgp in boxCandidates[loop[n]].EveryDiagonalPattern())
        {
            current.Add(loop[n], mgp);
            if (TryEveryPattern(strategyUser, possibilities, loop, boxCandidates, current, n + 1)) return true;
            current.Remove(loop[n]);
        }
        
        return false;
    }

    private bool Search(ISudokuStrategyUser strategyUser, int[] possibilities, BoxLoop loop,
        Dictionary<int, MiniGridPositions> boxCandidates)
    {
        var pp = new ParityPair[loop.Length];

        pp[0] = new ParityPair(Parity.Up, GetParityTransfer(boxCandidates[loop[0]])
                                          == ParityTransfer.Opposite ? Parity.Down : Parity.Up);

        for (int i = 1; i < pp.Length; i++)
        {
            if (loop[i] / 3 == loop[i - 1] / 3)
            {
                var r = pp[i - 1].RowParity;
                var c = GetParityTransfer(boxCandidates[loop[i]]) == ParityTransfer.Opposite
                    ? OppositeParity(r)
                    : r;
                pp[i] = new ParityPair(r, c);
            }
            else if (loop[i] % 3 == loop[i - 1] % 3)
            {
                var c = pp[i - 1].ColumnParity;
                var r = GetParityTransfer(boxCandidates[loop[i]]) == ParityTransfer.Opposite
                    ? OppositeParity(c)
                    : c;
                pp[i] = new ParityPair(r, c);
            }
            else throw new Exception();
        }

        if (loop[0] / 3 == loop[^1] / 3)
        {
            if (pp[0].RowParity == pp[^1].RowParity) return false; //Valid pattern
        }
        else if (loop[0] % 3 == loop[^1] % 3)
        {
            if (pp[0].ColumnParity == pp[^1].ColumnParity) return false; //Valid pattern
        }
        else throw new Exception();

        List<CellPossibility> notInPattern = new();
        List<Cell> cells = new();
        foreach (var mgp in boxCandidates.Values)
        {
            foreach (var cell in mgp)
            {
                cells.Add(cell);
                foreach (var p in strategyUser.PossibilitiesAt(cell).EnumeratePossibilities())
                {
                    if(!possibilities.Contains(p)) notInPattern.Add(new CellPossibility(cell, p));
                }
            }
        }

        if (notInPattern.Count == 0) return false; //Should never happen
        if (notInPattern.Count == 1) strategyUser.ChangeBuffer.ProposeSolutionAddition(notInPattern[0]);
        else
        {
            strategyUser.PreComputer.Graphs.ConstructSimple(SudokuConstructRuleBank.CellStrongLink, SudokuConstructRuleBank.CellWeakLink,
                SudokuConstructRuleBank.UnitStrongLink, SudokuConstructRuleBank.UnitWeakLink);
            var linkGraph = strategyUser.PreComputer.Graphs.SimpleLinkGraph;

            foreach (var target in linkGraph.Neighbors(notInPattern[0]))
            {
                var ok = true;
                for (int i = 1; i < notInPattern.Count; i++)
                {
                    if (!linkGraph.AreNeighbors(notInPattern[i], target))
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok) strategyUser.ChangeBuffer.ProposePossibilityRemoval(target);
            }
        }

        if (strategyUser.ChangeBuffer.NotEmpty() && strategyUser.ChangeBuffer.Commit(
                new ThorsHammerReportBuilder(cells, notInPattern)) && StopOnFirstPush) return true;

        return false;
    }
    
    private ParityTransfer GetParityTransfer(MiniGridPositions mgp)
    {
        var first = -1;
        for (int i = 0; i < 3; i++)
        {
            if (mgp.Contains(i, 0))
            {
                first = i;
                break;
            }
        }

        if (first == -1) throw new Exception();

        var down = (first + 1) % 3;
        if (mgp.Contains(down, 1)) return ParityTransfer.Same;

        var up = first - 1 < 0 ? 2 : first - 1;
        if (mgp.Contains(up, 1)) return ParityTransfer.Opposite;

        throw new Exception();
    }

    private Parity OppositeParity(Parity p)
    {
        return (Parity)(((int)p + 1) % 2);
    }
}

public class BoxGraph : IEnumerable<int>
{
    private readonly int[] _links = new int[9];

    public void AddLink(int one, int two)
    {
        _links[one] |= 1 << two;
        _links[two] |= 1 << one;
    }
    
    public IEnumerable<int> GetLinks(int n)
    {
        var l = _links[n];
        if(l == 0) yield break;

        for (int i = 0; i < 9; i++)
        {
            if (((l >> i) & 1) > 0) yield return i;
        }
    }

    public bool Contains(int n)
    {
        return _links[n] != 0;
    }

    public IEnumerator<int> GetEnumerator()
    {
        for (int i = 0; i < 9; i++)
        {
            if (_links[i] != 0) yield return i;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public interface IBoxLoopFinder
{
    public IEnumerable<BoxLoop> FindLoops(BoxGraph graph);
}

public class TwoByTwoLoopFinder : IBoxLoopFinder
{
    public IEnumerable<BoxLoop> FindLoops(BoxGraph graph)
    {
        var a = SmallSquare(graph, 0);
        if (a is not null) yield return a;
        
        a = SmallSquare(graph, 1);
        if (a is not null) yield return a;
        
        a = SmallSquare(graph, 3);
        if (a is not null) yield return a;
        
        a = SmallSquare(graph, 4);
        if (a is not null) yield return a;
        
        a = BigSquare(graph);
        if (a is not null) yield return a;
        
        a = RowRectangle(graph, 0);
        if (a is not null) yield return a;
        
        a = RowRectangle(graph, 3);
        if (a is not null) yield return a;
        
        a = ColumnRectangle(graph, 0);
        if (a is not null) yield return a;
        
        a = ColumnRectangle(graph, 1);
        if (a is not null) yield return a;
    }

    private BoxLoop? SmallSquare(BoxGraph graph, int start)
    {
        var array = new[]{ start, start + 1, start + 4, start + 3 };
        foreach (var i in array)
        {
            if (!graph.Contains(i)) return null;
        }

        return new BoxLoop(array);
    }

    private BoxLoop? BigSquare(BoxGraph graph)
    {
        var array = new[]{ 0, 2, 8, 6 };
        foreach (var i in array)
        {
            if (!graph.Contains(i)) return null;
        }

        return new BoxLoop(array);
    }
    
    private BoxLoop? RowRectangle(BoxGraph graph, int start)
    {
        var array = new[]{ start, start + 2, start + 5, start + 3 };
        foreach (var i in array)
        {
            if (!graph.Contains(i)) return null;
        }

        return new BoxLoop(array);
    }
    
    private BoxLoop? ColumnRectangle(BoxGraph graph, int start)
    {
        var array = new[]{ start, start + 1, start + 7, start + 6 };
        foreach (var i in array)
        {
            if (!graph.Contains(i)) return null;
        }

        return new BoxLoop(array);
    }
}

public class FullBoxLoopFinder : IBoxLoopFinder
{
    public IEnumerable<BoxLoop> FindLoops(BoxGraph graph)
    {
        HashSet<BoxLoop> result = new();

        var occupiedRow = new int[3];
        var occupiedColumn = new int[3];
        foreach (var start in graph)
        {
            var r = start / 3;
            var c = start % 3;
            
            occupiedRow[r] += 1;
            occupiedColumn[c] += 1;
            
            FindLoops(graph, result, new List<int> { start }, occupiedRow, occupiedColumn);
            
            occupiedRow[r] -= 1;
            occupiedColumn[c] -= 1;
        }

        return result;
    }
    
    private void FindLoops(BoxGraph graph, HashSet<BoxLoop> result, List<int> current, int[] occupiedRow, int[] occupiedCol)
    {
        var last = current[^1];

        foreach (var friend in graph.GetLinks(last))
        {
            if (friend == current[0] && current.Count >= 4)
            {
                result.Add(new BoxLoop(current.ToArray()));
                continue;
            }
            
            var r = friend / 3;
            var c = friend % 3;
            if (occupiedRow[r] >= 2 || occupiedCol[c] >= 2) continue;

            if (!current.Contains(friend))
            {
                occupiedRow[r] += 1;
                occupiedCol[c] += 1;
                current.Add(friend);
                
                FindLoops(graph, result, current, occupiedRow, occupiedCol);

                occupiedRow[r] -= 1;
                occupiedCol[c] -= 1;
                current.RemoveAt(current.Count - 1);
            }
        }
    }
}

public class BoxLoop
{
    private readonly int[] _array;

    public int Length => _array.Length;

    public BoxLoop(int[] array)
    {
        _array = array;
    }

    public int this[int index] => _array[index];

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var n in _array)
        {
            hash ^= n;
        }

        return hash;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BoxLoop bl || Length != _array.Length) return false;
        var lp = new LinePositions();
        foreach (var n in _array)
        {
            lp.Add(n);
        }

        foreach (var n in bl._array)
        {
            if (!lp.Contains(n)) return false;
        }

        return true;
    }
}

public enum Parity
{
    Up, Down
}

public readonly struct ParityPair
{
    public ParityPair(Parity rowParity, Parity columnParity)
    {
        RowParity = rowParity;
        ColumnParity = columnParity;
    }

    public Parity RowParity { get; }
    public Parity ColumnParity { get; }
}

public enum ParityTransfer
{
    Same, Opposite
}

public class ThorsHammerReportBuilder : IChangeReportBuilder<IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly List<Cell> _cells;
    private readonly List<CellPossibility> _notInPattern;

    public ThorsHammerReportBuilder(List<Cell> cells, List<CellPossibility> notInPattern)
    {
        _cells = cells;
        _notInPattern = notInPattern;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>( "", lighter =>
        {
            foreach (var cell in _cells)
            {
                lighter.HighlightCell(cell, ChangeColoration.Neutral);
            }

            foreach (var cp in _notInPattern)
            {
                lighter.HighlightPossibility(cp, ChangeColoration.CauseOnOne);
            }
            
            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}