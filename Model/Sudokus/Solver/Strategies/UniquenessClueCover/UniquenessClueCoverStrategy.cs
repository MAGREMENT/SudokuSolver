﻿namespace Model.Sudokus.Solver.Strategies.UniquenessClueCover;

public class UniquenessClueCoverStrategy : SudokuStrategy
{
    public const string OfficialName = "Uniqueness Clue Cover";
    private const InstanceHandling DefaultInstanceHandling = InstanceHandling.FirstOnly;

    private readonly IPatternCollection[] _collections;
    private bool _needFilter = true;
    
    public UniquenessClueCoverStrategy(params IPatternCollection[] collections)
        : base(OfficialName, StepDifficulty.Hard, DefaultInstanceHandling)
    {
        UniquenessDependency = UniquenessDependency.FullyDependent;
        _collections = collections;
        foreach (var c in _collections)
        {
            c.Strategy = this;
        }
    }

    
    public override void Apply(ISudokuStrategyUser strategyUser)
    {
        foreach (var c in _collections)
        {
            if (_needFilter)
            {
                if (c.Filter(strategyUser)) return;
            }
            else if (c.Apply(strategyUser)) return;
        }

        _needFilter = false;
    }

    public override void OnNewSudoku(IReadOnlySudoku s)
    {
        _needFilter = true;
    }
}