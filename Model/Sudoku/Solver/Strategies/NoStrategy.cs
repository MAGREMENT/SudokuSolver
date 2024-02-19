﻿namespace Model.Sudoku.Solver.Strategies;

public class NoStrategy : SudokuStrategy
{
    public const string OfficialName = "No Strategy";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;
    
    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;
    public NoStrategy() : base(OfficialName, StrategyDifficulty.None, DefaultBehavior){}

    public override void Apply(IStrategyUser strategyUser)
    {
        
    }
}