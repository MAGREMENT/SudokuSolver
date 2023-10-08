﻿using System.Linq;
using Model.Solver.StrategiesUtil;
using Model.Solver.StrategiesUtil.LinkGraph;

namespace Model.Solver.Strategies.AlternatingChains.ChainTypes;

public class SimpleAlternatingInferenceChains : IAlternatingChainType<CellPossibility>
{
    public const string OfficialName = "Alternating Inference Chains";
    
    public string Name => OfficialName;
    public StrategyDifficulty Difficulty => StrategyDifficulty.Extreme;
    public IStrategy? Strategy { get; set; }

    public LinkGraph<CellPossibility> GetGraph(IStrategyManager view)
    {
        view.GraphManager.ConstructSimple(ConstructRule.CellStrongLink, ConstructRule.CellWeakLink,
            ConstructRule.UnitStrongLink, ConstructRule.UnitWeakLink);
        return view.GraphManager.SimpleLinkGraph;
    }

    public bool ProcessFullLoop(IStrategyManager view, Loop<CellPossibility> loop)
    {
        bool wasProgressMade = false;
        
        loop.ForEachLink((one, two)
            => ProcessWeakLink(view, one, two, out wasProgressMade), LinkStrength.Weak);
        return wasProgressMade;
    }

    private void ProcessWeakLink(IStrategyManager view, CellPossibility one, CellPossibility two, out bool wasProgressMade)
    {
        if (one.Row == two.Row && one.Col == two.Col)
        {
            if (RemoveAllExcept(view, one.Row, one.Col, one.Possibility, two.Possibility)) wasProgressMade = true;
        }
        else
        {
            foreach (var coord in one.SharedSeenCells(two))
            {
                if (view.RemovePossibility(one.Possibility, coord.Row, coord.Col, Strategy!)) wasProgressMade = true;
            }   
        }

        wasProgressMade = false;
    }
    
    private bool RemoveAllExcept(IStrategyManager strategyManager, int row, int col, params int[] except)
    {
        bool wasProgressMade = false;
        foreach (var possibility in strategyManager.PossibilitiesAt(row, col))
        {
            if (!except.Contains(possibility))
            {
                if (strategyManager.RemovePossibility(possibility, row, col, Strategy!)) wasProgressMade = true;
            }
        }

        return wasProgressMade;
    }

    public bool ProcessWeakInference(IStrategyManager view, CellPossibility inference, Loop<CellPossibility> loop)
    {
        return view.RemovePossibility(inference.Possibility, inference.Row, inference.Col, Strategy!);
    }

    public bool ProcessStrongInference(IStrategyManager view, CellPossibility inference, Loop<CellPossibility> loop)
    {
        return view.AddSolution(inference.Possibility, inference.Row, inference.Col, Strategy!);
    }
}