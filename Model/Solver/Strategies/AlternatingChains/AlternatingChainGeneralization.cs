﻿using System;
using System.Collections.Generic;
using Global.Enums;
using Model.Solver.Helpers.Changes;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.Strategies.AlternatingChains;

public class AlternatingChainGeneralization<T> : AbstractStrategy, ICustomCommitComparer where T : ILinkGraphElement
{
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.ChooseBest;
    
    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;
    
    private readonly IAlternatingChainType<T> _chain;
    private readonly IAlternatingChainAlgorithm<T> _algorithm;

    public AlternatingChainGeneralization(IAlternatingChainType<T> chainType, IAlternatingChainAlgorithm<T> algo)
        : base(chainType.Name, chainType.Difficulty, DefaultBehavior)
    {
        _chain = chainType;
        _chain.Strategy = this;
        _algorithm = algo;
    }

    public override void Apply(IStrategyManager strategyManager)
    {
        _algorithm.Run(strategyManager, _chain.GetGraph(strategyManager), _chain);
    }

    public int Compare(ChangeCommit first, ChangeCommit second)
    {
        if (first.Builder is not AlternatingChainReportBuilder<T> r1 ||
            second.Builder is not AlternatingChainReportBuilder<T> r2) return 0;

        var rankDiff = r2.Loop.MaxRank() - r1.Loop.MaxRank();
        return rankDiff == 0 ? r2.Loop.Count - r1.Loop.Count : rankDiff;
    }
}

public interface IAlternatingChainType<T> where T : ILinkGraphElement
{
    public string Name { get; }
    public StrategyDifficulty Difficulty { get; }
    IStrategy? Strategy { set; get; }
    
    LinkGraph<T> GetGraph(IStrategyManager view);

    bool ProcessFullLoop(IStrategyManager view, LinkGraphLoop<T> loop);

    bool ProcessWeakInference(IStrategyManager view, T inference, LinkGraphLoop<T> loop);

    bool ProcessStrongInference(IStrategyManager view, T inference, LinkGraphLoop<T> loop);
}

public interface IAlternatingChainAlgorithm<T> where T : ILinkGraphElement
{
    void Run(IStrategyManager view, LinkGraph<T> graph, IAlternatingChainType<T> chainType);
}

public class AlternatingChainReportBuilder<T> : IChangeReportBuilder where T : ILinkGraphElement
{
    public LinkGraphLoop<T> Loop { get; }
    private readonly LoopType _type;

    public AlternatingChainReportBuilder(LinkGraphLoop<T> loop, LoopType type)
    {
        Loop = loop;
        _type = type;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), Explanation(),
            lighter =>
            {
                var coloring = Loop.Links[0] == LinkStrength.Strong
                    ? ChangeColoration.CauseOffOne
                    : ChangeColoration.CauseOnOne;
                
                foreach (var element in Loop)
                {
                    lighter.HighlightLinkGraphElement(element, coloring);
                    coloring = coloring == ChangeColoration.CauseOnOne
                        ? ChangeColoration.CauseOffOne
                        : ChangeColoration.CauseOnOne;
                }
                
                Loop.ForEachLink((one, two) => lighter.CreateLink(one, two, LinkStrength.Strong), LinkStrength.Strong);
                Loop.ForEachLink((one, two) => lighter.CreateLink(one, two, LinkStrength.Weak), LinkStrength.Weak);
                
                IChangeReportBuilder.HighlightChanges(lighter, changes);
            });
    }

    private string Explanation()
    {
        var result = _type switch
        {
            LoopType.NiceLoop => "Nice loop",
            LoopType.StrongInference => "Loop with a strong inference",
            LoopType.WeakInference => "Loop with a weak inference",
            _ => throw new ArgumentOutOfRangeException()
        };

        return result + $" found\nLoop :: {Loop}";
    }
}

public enum LoopType
{
    NiceLoop, WeakInference, StrongInference
}

