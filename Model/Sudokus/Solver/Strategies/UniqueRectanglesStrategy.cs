using System.Collections.Generic;
using Model.Core;
using Model.Core.Changes;
using Model.Core.Highlighting;
using Model.Core.Settings.Types;
using Model.Sudokus.Solver.PossibilityPosition;
using Model.Sudokus.Solver.Utility;
using Model.Sudokus.Solver.Utility.Graphs;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Sudokus.Solver.Strategies;

public class UniqueRectanglesStrategy : SudokuStrategy
{
    public const string OfficialName = "Unique Rectangles";
    private const InstanceHandling DefaultInstanceHandling = InstanceHandling.FirstOnly;

    private readonly BooleanSetting _allowMissingCandidates;
    
    public UniqueRectanglesStrategy(bool allowMissingCandidates) : base(OfficialName, StepDifficulty.Hard, DefaultInstanceHandling)
    {
        _allowMissingCandidates = new BooleanSetting("Missing candidates allowed", allowMissingCandidates);
        UniquenessDependency = UniquenessDependency.FullyDependent;
        AddSetting(_allowMissingCandidates);
    }
    
    public override void Apply(ISudokuSolverData solverData)
    {
        Dictionary<BiValue, List<Cell>> biValueMap = new();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var possibilities = solverData.PossibilitiesAt(row, col);
                if (possibilities.Count != 2) continue;

                var asArray = possibilities.ToArray();
                var biValue = new BiValue(asArray[0], asArray[1]);
                var current = new Cell(row, col);

                if (!biValueMap.TryGetValue(biValue, out var list))
                {
                    list = new List<Cell>();
                    biValueMap[biValue] = list;
                }
                else
                {
                    foreach (var cell in list)
                    {
                        if (Search(solverData, biValue, cell, current)) return;
                    }
                }

                list.Add(current);
            }
        }

        foreach (var entry in biValueMap)
        {
            foreach (var cell in entry.Value)
            {
                if (SearchHidden(solverData, entry.Key, cell)) return;
            }
        }
    }

    private bool Search(ISudokuSolverData solverData, BiValue values, params Cell[] floor)
    {
        foreach (var roof in SudokuCellUtility.DeadlyPatternRoofs(floor))
        {
            if (Try(solverData, values, floor, roof)) return true;
        }

        return false;
    }

    private bool Try(ISudokuSolverData solverData, BiValue values, Cell[] floor, params Cell[] roof)
    {
        var roofOnePossibilities = solverData.PossibilitiesAt(roof[0]);
        var roofTwoPossibilities = solverData.PossibilitiesAt(roof[1]);

        if (!ValidateRoof(solverData, values, roof[0], ref roofOnePossibilities) ||
            !ValidateRoof(solverData, values, roof[1], ref roofTwoPossibilities)) return false;
        
        //Type 1
        if (values == roofOnePossibilities)
        {
            solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, roof[1]);
            solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, roof[1]);
        }
        else if (values == roofTwoPossibilities)
        {
            solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, roof[0]);
            solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, roof[0]);
        }

        if (solverData.ChangeBuffer.NotEmpty() && solverData.ChangeBuffer.Commit(
                new UniqueRectanglesReportBuilder(floor, roof)) &&
                    StopOnFirstPush) return true;
        
        //Type 2
        if (roofOnePossibilities.Count == 3 && roofTwoPossibilities.Count == 3 &&
            roofOnePossibilities.Equals(roofTwoPossibilities))
        {
            foreach (var possibility in roofOnePossibilities.EnumeratePossibilities())
            {
                if (possibility == values.One || possibility == values.Two) continue;

                foreach (var cell in SudokuCellUtility.SharedSeenCells(roof[0], roof[1]))
                {
                    solverData.ChangeBuffer.ProposePossibilityRemoval(possibility, cell);
                }
            }
        }
        
        if (solverData.ChangeBuffer.NotEmpty() && solverData.ChangeBuffer.Commit(
                new UniqueRectanglesReportBuilder(floor, roof)) &&
                    StopOnFirstPush) return true;
        
        //Type 3
        var notBiValuePossibilities = roofOnePossibilities | roofTwoPossibilities;
        notBiValuePossibilities -= values.One;
        notBiValuePossibilities -= values.Two;

        var ssc = new List<Cell>(SudokuCellUtility.SharedSeenCells(roof[0], roof[1]));
        foreach (var als in solverData.AlmostNakedSetSearcher.InCells(ssc))
        {
            if (!als.Possibilities.ContainsAll(notBiValuePossibilities)) continue;

            ProcessUrWithAls(solverData, roof, als);
            if (solverData.ChangeBuffer.NotEmpty() && solverData.ChangeBuffer.Commit(
                    new UniqueRectanglesWithAlmostLockedSetReportBuilder(floor, roof, als)) &&
                        StopOnFirstPush) return true;
        }

        //Type 4 & 5
        bool oneOk = false;
        bool twoOke = false;
        if (roof[0].Row == roof[1].Row || roof[0].Column == roof[1].Column)
        {
            //Type 4
            foreach (var cell in SudokuCellUtility.SharedSeenCells(roof[0], roof[1]))
            {
                if (solverData.PossibilitiesAt(cell).Contains(values.One)) oneOk = true;
                if (solverData.PossibilitiesAt(cell).Contains(values.Two)) twoOke = true;

                if (oneOk && twoOke) break;
            }
            
            if (!oneOk)
            {
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, roof[0]);
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, roof[1]);
            }
            else if (!twoOke)
            {
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, roof[0]);
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, roof[1]);
            }
        }
        else
        {
            //Type 5
            for (int unit = 0; unit < 9; unit++)
            {
                if (unit != roof[0].Column && unit != roof[1].Column)
                {
                    if (solverData.PossibilitiesAt(roof[0].Row, unit).Contains(values.One)) oneOk = true;
                    if (solverData.PossibilitiesAt(roof[0].Row, unit).Contains(values.Two)) twoOke = true;
                    
                    if (solverData.PossibilitiesAt(roof[1].Row, unit).Contains(values.One)) oneOk = true;
                    if (solverData.PossibilitiesAt(roof[1].Row, unit).Contains(values.Two)) twoOke = true;
                }
                
                if (unit != roof[0].Row && unit != roof[1].Row)
                {
                    if (solverData.PossibilitiesAt(unit, roof[0].Column).Contains(values.One)) oneOk = true;
                    if (solverData.PossibilitiesAt(unit, roof[0].Column).Contains(values.Two)) twoOke = true;
                    
                    if (solverData.PossibilitiesAt(unit, roof[1].Column).Contains(values.One)) oneOk = true;
                    if (solverData.PossibilitiesAt(unit, roof[1].Column).Contains(values.Two)) twoOke = true;
                }
                
                if (oneOk && twoOke) break;
            }
            
            if (!oneOk)
            {
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, floor[0]);
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, floor[1]);
            }
            else if (!twoOke)
            {
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, floor[0]);
                solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, floor[1]);
            }
        }
        
        if (solverData.ChangeBuffer.NotEmpty() && solverData.ChangeBuffer.Commit(
                new UniqueRectanglesReportBuilder(floor, roof)) &&
                    StopOnFirstPush) return true;
        
        //Type 6 (aka hidden type 2)
        if (roof[0].Row == roof[1].Row || roof[0].Column == roof[1].Column)
        {
            solverData.PreComputer.Graphs.ConstructSimple(SudokuConstructRuleBank.UnitStrongLink);
            var graph = solverData.PreComputer.Graphs.SimpleLinkGraph;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    var cpf1 = new CellPossibility(floor[i], values.One);
                    var cpr1 = new CellPossibility(roof[j], values.One);

                    var cpf2 = new CellPossibility(floor[i], values.Two);
                    var cpr2 = new CellPossibility(roof[j], values.Two);
                
                    if (graph.AreNeighbors(cpr1, cpf1, LinkStrength.Strong))
                    {
                        solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, roof[(j + 1) % 2]);
                        if (solverData.ChangeBuffer.Commit( new UniqueRectanglesWithStrongLinkReportBuilder(
                                floor, roof, new Link<CellPossibility>(cpr1, cpf1)))
                                    && StopOnFirstPush) return true;
                    }
                
                    if (graph.AreNeighbors(cpr2, cpf2, LinkStrength.Strong))
                    {
                        solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, roof[(j + 1) % 2]);
                        if (solverData.ChangeBuffer.Commit( new UniqueRectanglesWithStrongLinkReportBuilder(
                                floor, roof, new Link<CellPossibility>(cpr2, cpf2)))
                                    && StopOnFirstPush) return true;
                    }
                }
            }
        }

        return false;
    }

    private void ProcessUrWithAls(ISudokuSolverData solverData, Cell[] roof, IPossibilitiesPositions als)
    {
        List<Cell> buffer = new();
        foreach (var possibility in als.Possibilities.EnumeratePossibilities())
        {
            foreach (var cell in als.EachCell())
            {
                if(solverData.PossibilitiesAt(cell).Contains(possibility)) buffer.Add(cell);
            }

            foreach (var r in roof)
            {
                if (solverData.PossibilitiesAt(r).Contains(possibility)) buffer.Add(r);
            }

            foreach (var cell in SudokuCellUtility.SharedSeenCells(buffer))
            {
                solverData.ChangeBuffer.ProposePossibilityRemoval(possibility, cell);
            }
            
            buffer.Clear();
        }
    }

    private bool SearchHidden(ISudokuSolverData solverData, BiValue values, Cell cell)
    {
        for (int row = 0; row < 9; row++)
        {
            if(row == cell.Row) continue;
            var rowPossibilities = solverData.PossibilitiesAt(row, cell.Column);

            var oneWasChanged = false;
            var twoWasChanged = false;
            if (!ValidateRoof(solverData, values, new Cell(row, cell.Column), ref rowPossibilities,
                    ref oneWasChanged, ref twoWasChanged) || (oneWasChanged && twoWasChanged)) continue;

            for (int col = 0; col < 9; col++)
            {
                if (col == cell.Column || !SudokuCellUtility.AreSpreadOverTwoBoxes(row, col, cell.Row, cell.Column)) continue;
                var colPossibilities = solverData.PossibilitiesAt(cell.Row, col);

                var oneWasChangedCopy = oneWasChanged;
                var twoWasChangedCopy = twoWasChanged;
                if (!ValidateRoof(solverData, values, new Cell(cell.Row, col), ref colPossibilities,
                        ref oneWasChangedCopy, ref twoWasChangedCopy)) continue;

                var opposite = new Cell(row, col);
                var oppositePossibilities = solverData.PossibilitiesAt(opposite);
                
                if (!oppositePossibilities.Contains(values.One) || !oppositePossibilities.Contains(values.Two)) continue;

                if (!oneWasChangedCopy && solverData.RowPositionsAt(row, values.One).Count == 2 &&
                    solverData.ColumnPositionsAt(col, values.One).Count == 2)
                {
                    solverData.ChangeBuffer.ProposePossibilityRemoval(values.Two, opposite);
                    if (solverData.ChangeBuffer.Commit( new HiddenUniqueRectanglesReportBuilder(
                            cell, opposite, values.One)) && StopOnFirstPush) return true;
                }
                
                if (!twoWasChangedCopy && solverData.RowPositionsAt(row, values.Two).Count == 2 &&
                    solverData.ColumnPositionsAt(col, values.Two).Count == 2)
                {
                    solverData.ChangeBuffer.ProposePossibilityRemoval(values.One, opposite);
                    if (solverData.ChangeBuffer.Commit( new HiddenUniqueRectanglesReportBuilder(
                            cell, opposite, values.One)) && StopOnFirstPush) return true;
                }
            }
        }
        
        return false;
    }

    private bool ValidateRoof(ISudokuSolverData solverData, BiValue biValue, Cell cell, ref ReadOnlyBitSet16 possibilities)
    {
        if (!possibilities.Contains(biValue.One))
        {
            if (_allowMissingCandidates.Value && solverData.RawPossibilitiesAt(cell).Contains(biValue.One))
            {
                possibilities += biValue.One;
            }
            else return false;
        }
        
        if (!possibilities.Contains(biValue.Two))
        {
            if (_allowMissingCandidates.Value && solverData.RawPossibilitiesAt(cell).Contains(biValue.Two))
            {
                possibilities += biValue.Two;
            }
            else return false;
        }
        
        return true;
    }
    
    private bool ValidateRoof(ISudokuSolverData solverData, BiValue biValue, Cell cell,
        ref ReadOnlyBitSet16 possibilities, ref bool oneWasChanged, ref bool twoWasChanged)
    {
        if (!possibilities.Contains(biValue.One))
        {
            if (_allowMissingCandidates.Value && solverData.RawPossibilitiesAt(cell).Contains(biValue.One))
            {
                possibilities += biValue.One;
                oneWasChanged = true;
            }
            else return false;
        }
        
        if (!possibilities.Contains(biValue.Two))
        {
            if (_allowMissingCandidates.Value && solverData.RawPossibilitiesAt(cell).Contains(biValue.Two))
            {
                possibilities += biValue.Two;
                twoWasChanged = true;
            }
            else return false;
        }
        
        return true;
    }
}

public readonly struct BiValue
{
    public BiValue(int one, int two)
    {
        One = one;
        Two = two;
    }

    public int One { get; }
    public int Two { get; }

    public override int GetHashCode()
    {
        return One ^ Two;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BiValue bi) return false;
        return (bi.One == One && bi.Two == Two) || (bi.One == Two && bi.Two == One);
    }

    public override string ToString()
    {
        return $"Bi-Value : {One}, {Two}";
    }

    public static bool operator ==(BiValue left, BiValue right)
    {
        return (left.One == right.One && left.Two == right.Two) || (left.One == right.Two && left.Two == right.One);
    }

    public static bool operator !=(BiValue left, BiValue right)
    {
        return !(left == right);
    }
}

public class UniqueRectanglesReportBuilder : IChangeReportBuilder<NumericChange, IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly Cell[] _floor;
    private readonly Cell[] _roof;

    public UniqueRectanglesReportBuilder(Cell[] floor, Cell[] roof)
    {
        _floor = floor;
        _roof = roof;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>( "", lighter =>
        {
            foreach (var floor in _floor)
            {
                lighter.HighlightCell(floor, ChangeColoration.CauseOffTwo);
            }

            foreach (var roof in _roof)
            {
                lighter.HighlightCell(roof, snapshot.PossibilitiesAt(roof).Count == 2 ? 
                    ChangeColoration.CauseOffTwo : ChangeColoration.CauseOffOne);
            }
            
            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}

public class UniqueRectanglesWithStrongLinkReportBuilder : IChangeReportBuilder<NumericChange, IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly Cell[] _floor;
    private readonly Cell[] _roof;
    private readonly Link<CellPossibility> _link;

    public UniqueRectanglesWithStrongLinkReportBuilder(Cell[] floor, Cell[] roof, Link<CellPossibility> link)
    {
        _floor = floor;
        _roof = roof;
        _link = link;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>( "", lighter =>
        {
            foreach (var floor in _floor)
            {
                lighter.HighlightCell(floor, ChangeColoration.CauseOffTwo);
            }

            foreach (var roof in _roof)
            {
                lighter.HighlightCell(roof, ChangeColoration.CauseOffOne);
            }

            lighter.CreateLink(_link.From, _link.To, LinkStrength.Strong);
            
            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}

public class UniqueRectanglesWithAlmostLockedSetReportBuilder : IChangeReportBuilder<NumericChange, IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly Cell[] _floor;
    private readonly Cell[] _roof;
    private readonly IPossibilitiesPositions _als;

    public UniqueRectanglesWithAlmostLockedSetReportBuilder(Cell[] floor, Cell[] roof, IPossibilitiesPositions als)
    {
        _floor = floor;
        _roof = roof;
        _als = als;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>( "", lighter =>
        {
            foreach (var floor in _floor)
            {
                lighter.HighlightCell(floor, ChangeColoration.CauseOffTwo);
            }

            foreach (var roof in _roof)
            {
                lighter.HighlightCell(roof, ChangeColoration.CauseOffOne);
            }

            foreach (var cell in _als.EachCell())
            {
                lighter.HighlightCell(cell, ChangeColoration.CauseOffThree);
            }
            
            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}

public class HiddenUniqueRectanglesReportBuilder : IChangeReportBuilder<NumericChange, IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly Cell _initial;
    private readonly Cell _opposite;
    private readonly int _stronglyLinkedPossibility;

    public HiddenUniqueRectanglesReportBuilder(Cell initial, Cell opposite, int stronglyLinkedPossibility)
    {
        _initial = initial;
        _opposite = opposite;
        _stronglyLinkedPossibility = stronglyLinkedPossibility;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>( "", lighter =>
        {
            lighter.HighlightCell(_initial, ChangeColoration.CauseOffTwo);

            lighter.HighlightCell(_opposite, ChangeColoration.CauseOffOne);
            lighter.HighlightCell(_opposite.Row, _initial.Column, ChangeColoration.CauseOffOne);
            lighter.HighlightCell(_initial.Row, _opposite.Column, ChangeColoration.CauseOffOne);

            lighter.CreateLink(new CellPossibility(_opposite, _stronglyLinkedPossibility), new CellPossibility(
                _opposite.Row, _initial.Column, _stronglyLinkedPossibility), LinkStrength.Strong);
            lighter.CreateLink(new CellPossibility(_opposite, _stronglyLinkedPossibility), new CellPossibility(
                _initial.Row, _opposite.Column, _stronglyLinkedPossibility), LinkStrength.Strong);
            
            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<NumericChange> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}