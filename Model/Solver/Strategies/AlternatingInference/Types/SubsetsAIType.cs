﻿using System.Collections.Generic;
using Global;
using Global.Enums;
using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.AlmostLockedSets;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.Strategies.AlternatingInference.Types;

public class SubsetsAIType : IAlternatingInferenceType<ILinkGraphElement>
{
    public const string OfficialLoopName = "Subsets Alternating Inference Loops";
    public const string OfficialChainName = "Subsets Alternating Inference Chains";

    public string LoopName => OfficialLoopName;
    public string ChainName => OfficialChainName;
    public StrategyDifficulty Difficulty => StrategyDifficulty.Extreme;
    public IStrategy? Strategy { get; set; }
    public LinkGraph<ILinkGraphElement> GetGraph(IStrategyManager strategyManager)
    {
        strategyManager.GraphManager.ConstructComplex(ConstructRule.CellStrongLink, ConstructRule.CellWeakLink,
            ConstructRule.UnitStrongLink, ConstructRule.UnitWeakLink, ConstructRule.PointingPossibilities,
            ConstructRule.AlmostNakedPossibilities, ConstructRule.JuniorExocet);
        return strategyManager.GraphManager.ComplexLinkGraph;
    }

    public bool ProcessFullLoop(IStrategyManager strategyManager, LinkGraphLoop<ILinkGraphElement> loop)
    {
        loop.ForEachLink((one, two) => ProcessWeakLink(strategyManager, one, two), LinkStrength.Weak);

        return strategyManager.ChangeBuffer.Commit(Strategy!, new AlternatingInferenceLoopReportBuilder<ILinkGraphElement>(loop, LoopType.NiceLoop));
    }

    private void ProcessWeakLink(IStrategyManager view, ILinkGraphElement one, ILinkGraphElement two)
    {
        var cp1 = one.EveryCellPossibilities();
        var pos1 = one.EveryPossibilities();
        var cp2 = two.EveryCellPossibilities();
        var pos2 = two.EveryPossibilities();

        if (cp1.Length == 1 && cp2.Length == 1 && pos1.Count == 1 && pos2.Count == 1 && cp1[0].Cell == cp2[0].Cell)
        {
            foreach (var possibility in view.PossibilitiesAt(cp1[0].Cell))
            {
                if (pos1.Peek(possibility) || pos2.Peek(possibility)) continue;
                
                view.ChangeBuffer.ProposePossibilityRemoval(possibility, cp1[0].Cell.Row, cp1[0].Cell.Column);
            }

            return;
        }

        var and = pos1.And(pos2);

        foreach (var possibility in and)
        {
            List<Cell> cells = new();
            
            foreach (var cp in cp1)
            {
                if (cp.Possibilities.Peek(possibility)) cells.Add(cp.Cell);
            }
            
            foreach (var cp in cp2)
            {
                if (cp.Possibilities.Peek(possibility)) cells.Add(cp.Cell);
            }

            foreach (var cell in Cells.SharedSeenCells(cells))
            {
                view.ChangeBuffer.ProposePossibilityRemoval(possibility, cell.Row, cell.Column);
            }
        }

        if (one is NakedSet ans && two is CellPossibility cellPossibility)
        {
            foreach (var possibility in ans.EveryPossibilities())
            {
                if (possibility == cellPossibility.Possibility) continue;
                
                List<Cell> cells = new();

                foreach (var cp in ans.EveryCellPossibilities())
                {
                    if (cp.Possibilities.Peek(possibility)) cells.Add(cp.Cell);
                }
                
                foreach (var cell in Cells.SharedSeenCells(cells))
                {
                    view.ChangeBuffer.ProposePossibilityRemoval(possibility, cell.Row, cell.Column);
                }
            }
        }
    }

    public bool ProcessWeakInferenceLoop(IStrategyManager strategyManager, ILinkGraphElement inference, LinkGraphLoop<ILinkGraphElement> loop)
    {
        if (inference is not CellPossibility pos) return false;
        strategyManager.ChangeBuffer.ProposePossibilityRemoval(pos.Possibility, pos.Row, pos.Column);

        return strategyManager.ChangeBuffer.Commit(Strategy!, new AlternatingInferenceLoopReportBuilder<ILinkGraphElement>(loop, LoopType.WeakInference));
    }

    public bool ProcessStrongInferenceLoop(IStrategyManager strategyManager, ILinkGraphElement inference, LinkGraphLoop<ILinkGraphElement> loop)
    {
        if (inference is not CellPossibility pos) return false;
        strategyManager.ChangeBuffer.ProposeSolutionAddition(pos.Possibility, pos.Row, pos.Column);

        return strategyManager.ChangeBuffer.Commit(Strategy!, new AlternatingInferenceLoopReportBuilder<ILinkGraphElement>(loop, LoopType.StrongInference));
    }

    public bool ProcessChain(IStrategyManager strategyManager, LinkGraphChain<ILinkGraphElement> chain, LinkGraph<ILinkGraphElement> graph)
    {
        return IAlternatingInferenceType<ILinkGraphElement>.ProcessChainWithComplexGraph(strategyManager,
            chain, graph, Strategy!);
    }
}