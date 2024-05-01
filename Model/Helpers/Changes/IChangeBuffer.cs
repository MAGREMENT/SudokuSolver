﻿using System.Collections.Generic;
using Model.Helpers.Highlighting;
using Model.Sudokus.Solver.Utility;
using Model.Utility;

namespace Model.Helpers.Changes;

/// <summary>
/// Strategies are most often dependant on the fact that the board stay the same through the search. It is then
/// important that no change are executed outside of the Push() method, which signals that a strategy has stopped
/// searching.
/// </summary>
public interface IChangeBuffer<out TVerifier, THighlighter> where TVerifier : IUpdatableSolvingState where THighlighter : ISolvingStateHighlighter
{
    public bool IsManagingSteps { get; }
    
    public void ProposePossibilityRemoval(int possibility, Cell cell)
    {
        ProposePossibilityRemoval(new CellPossibility(cell, possibility));
    }

    public void ProposePossibilityRemoval(int possibility, int row, int col)
    {
        ProposePossibilityRemoval(new CellPossibility(row, col, possibility));
    }
    
    public void ProposePossibilityRemoval(CellPossibility cp);
    
    public void ProposeSolutionAddition(int number, int row, int col)
    {
        ProposeSolutionAddition(new CellPossibility(row, col, number));
    }
    
    public void ProposeSolutionAddition(int number, Cell cell)
    {
        ProposeSolutionAddition(new CellPossibility(cell, number));
    }

    public void ProposeSolutionAddition(CellPossibility cp);

    public bool NotEmpty();

    public bool Commit(IChangeReportBuilder<TVerifier, THighlighter> builder);

    public void Push(ICommitMaker pusher);

    public void PushCommit(BuiltChangeCommit<THighlighter> commit);
}

public static class ChangeBufferHelper
{
    public static SolverProgress[] EstablishChangeList(HashSet<CellPossibility> solutionsAdded, HashSet<CellPossibility> possibilitiesRemoved)
    {
        var count = 0;
        var changes = new SolverProgress[solutionsAdded.Count + possibilitiesRemoved.Count];
        
        foreach (var solution in solutionsAdded)
        {
            changes[count++] = new SolverProgress(ProgressType.SolutionAddition, solution);
        }
        
        foreach (var possibility in possibilitiesRemoved)
        {
            changes[count++] = new SolverProgress(ProgressType.PossibilityRemoval, possibility);
        }
        
        solutionsAdded.Clear();
        possibilitiesRemoved.Clear();

        return changes;
    }
}

public interface IChangeCommit
{
    SolverProgress[] Changes { get; }

    bool TryGetBuilder<TBuilderType>(out TBuilderType builder) where TBuilderType : class;
}

public class ChangeCommit<TVerifier, THighlighter> : IChangeCommit 
    where TVerifier : ISolvingState where THighlighter : ISolvingStateHighlighter
{
    public SolverProgress[] Changes { get; }

    public IChangeReportBuilder<TVerifier, THighlighter> Builder { get; }

    public ChangeCommit(SolverProgress[] changes, IChangeReportBuilder<TVerifier, THighlighter> builder)
    {
        Changes = changes;
        Builder = builder;
    }
    
    public bool TryGetBuilder<TBuilderType>(out TBuilderType builder) where TBuilderType : class
    {
        builder = Builder as TBuilderType;
        return builder is not null;
    }
}

public class BuiltChangeCommit<THighlighter>
{
    public BuiltChangeCommit(ICommitMaker maker, SolverProgress[] changes, ChangeReport<THighlighter> report)
    {
        Maker = maker;
        Changes = changes;
        Report = report;
    }
    
    public ICommitMaker Maker { get; }
    public SolverProgress[] Changes { get; }
    public ChangeReport<THighlighter> Report { get; }
}