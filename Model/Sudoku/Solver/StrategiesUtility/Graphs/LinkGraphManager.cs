﻿using Model.Sudoku.Solver.StrategiesUtility.Graphs.ConstructRules;

namespace Model.Sudoku.Solver.StrategiesUtility.Graphs;

public class LinkGraphManager
{
    public ILinkGraph<ISudokuElement> ComplexLinkGraph { get; } = LinkGraphs.NewComplex();
    private long _rulesAppliedOnComplex;

    public ILinkGraph<CellPossibility> SimpleLinkGraph { get; } = LinkGraphs.NewSimple();
    private long _rulesAppliedOnSimple;

    private readonly IStrategyUser _solver;
    
    private readonly IConstructRule[] _rules =
    {
        new UnitStrongLinkConstructRule(),
        new CellStrongLinkConstructRule(),
        new UnitWeakLinkConstructRule(),
        new CellWeakLinkConstructRule(),
        new PointingPossibilitiesConstructRule(),
        new AlmostNakedSetConstructRule(),
        new XYChainSpecificConstructRule(),
        new JuniorExocetConstructRule()
    };

    public LinkGraphManager(IStrategyUser solver)
    {
        _solver = solver;
    }

    public void ConstructComplex(params ConstructRule[] rules)
    {
        if(IsOverConstructed(_rulesAppliedOnComplex, rules)) ClearComplex();
        foreach (var rule in rules)
        {
            DoConstructComplex(rule);
        }
    }

    public void ConstructSimple(params ConstructRule[] rules)
    {
        if(IsOverConstructed(_rulesAppliedOnSimple, rules)) ClearSimple();
        foreach (var rule in rules)
        {
            DoConstructSimple(rule);
        }
    }

    public void Clear()
    {
        ClearComplex();
        ClearSimple();
    }
    
    private void DoConstructComplex(ConstructRule rule)
    {
        int asInt = (int)rule;
        if(((_rulesAppliedOnComplex >> asInt) & 1) > 0) return;

        _rules[asInt].Apply(ComplexLinkGraph, _solver);
        _rulesAppliedOnComplex |= 1L << asInt;
    }

    private void DoConstructSimple(ConstructRule rule)
    {
        int asInt = (int)rule;
        if(((_rulesAppliedOnSimple >> asInt) & 1) > 0) return;

        _rules[asInt].Apply(SimpleLinkGraph, _solver);
        _rulesAppliedOnSimple |= 1L << asInt;
    }

    private void ClearComplex()
    {
        ComplexLinkGraph.Clear();
        _rulesAppliedOnComplex = 0;
    }

    private void ClearSimple()
    {
        SimpleLinkGraph.Clear();
        _rulesAppliedOnSimple = 0;
    }

    private bool IsOverConstructed(long rulesApplied, params ConstructRule[] rules)
    {
        var buffer = 0L;
        foreach (var rule in rules)
        {
            buffer |= 1L << (int)rule;
        }

        return (buffer | rulesApplied) != buffer;
    }
}

public enum ConstructRule
{
    UnitStrongLink = 0, CellStrongLink = 1, UnitWeakLink = 2, CellWeakLink = 3, 
    PointingPossibilities = 4, AlmostNakedPossibilities = 5, XYChainSpecific = 6,
    JuniorExocet = 7
}

public interface IConstructRule
{
    public void Apply(ILinkGraph<ISudokuElement> linkGraph, IStrategyUser strategyUser);
    
    public void Apply(ILinkGraph<CellPossibility> linkGraph, IStrategyUser strategyUser);
}