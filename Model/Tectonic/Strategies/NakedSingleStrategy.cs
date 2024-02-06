﻿using System.Collections.Generic;
using Model.Helpers.Changes;
using Model.Sudoku.Solver;

namespace Model.Tectonic.Strategies;

public class NakedSingleStrategy : AbstractTectonicStrategy
{
    public NakedSingleStrategy() : base("Naked Single", StrategyDifficulty.Basic, OnCommitBehavior.WaitForAll)
    {
    }
    
    public override void Apply(IStrategyUser strategyUser)
    {
        foreach (var cell in strategyUser.Tectonic.EachCell())
        {
            var p = strategyUser.PossibilitiesAt(cell);
            if (p.Count != 1) continue;
            
            strategyUser.ChangeBuffer.ProposeSolutionAddition(
                p.First(1, strategyUser.Tectonic.GetZone(cell).Count), cell);
            strategyUser.ChangeBuffer.Commit(new NakedSingleReportBuilder());
        }
    }
}

public class NakedSingleReportBuilder : IChangeReportBuilder
{
    public ChangeReport Build(IReadOnlyList<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return ChangeReport.Default(changes);
    }
}