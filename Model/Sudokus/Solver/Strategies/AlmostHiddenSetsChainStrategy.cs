﻿using System.Collections.Generic;
using Model.Core;
using Model.Core.Changes;
using Model.Core.Highlighting;
using Model.Sudokus.Solver.Position;
using Model.Sudokus.Solver.PossibilityPosition;
using Model.Sudokus.Solver.Utility;
using Model.Sudokus.Solver.Utility.Graphs;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Sudokus.Solver.Strategies;

public class AlmostHiddenSetsChainStrategy : SudokuStrategy
{
    public const string OfficialName = "Almost Hidden Sets Chain";
    private const InstanceHandling DefaultInstanceHandling = InstanceHandling.FirstOnly;

    private readonly bool _checkLength2;
    
    public AlmostHiddenSetsChainStrategy(bool checkLength2) : base(OfficialName, StepDifficulty.Extreme, DefaultInstanceHandling)
    {
        _checkLength2 = checkLength2;
    }

    
    public override void Apply(ISudokuSolverData solverData)
    {
        var graph = solverData.PreComputer.AlmostHiddenSetGraph();
        solverData.PreComputer.Graphs.ConstructSimple(SudokuConstructRuleBank.UnitStrongLink);
        var linkGraph = solverData.PreComputer.Graphs.SimpleLinkGraph;

        foreach (var start in graph)
        {
            if (Search(solverData, graph, start.Possibilities, new HashSet<IPossibilitiesPositions> {start},
                    new ChainBuilder<IPossibilitiesPositions, Cell>(start), linkGraph)) return;
        }
    }

    private bool Search(ISudokuSolverData solverData, PositionsGraph<IPossibilitiesPositions> graph,
        ReadOnlyBitSet16 occupied, HashSet<IPossibilitiesPositions> explored, ChainBuilder<IPossibilitiesPositions, Cell> chain,
        ILinkGraph<CellPossibility> linkGraph)
    {
        foreach (var friend in graph.GetLinks(chain.LastElement()))
        {
            /*if (chain.Count > 2 && chain.FirstElement().Equals(friend.To) &&
                 CheckForLoop(strategyManager, chain, friend.Cells)) return true;*/
            
            if (explored.Contains(friend.To) || occupied.ContainsAny(friend.To.Possibilities)) continue;

            var lastLink = chain.LastLink();
            foreach (var possibleLink in friend.Cells)
            {
                if (chain.Count > 0 && lastLink == possibleLink) continue;

                chain.Add(possibleLink, friend.To);
                explored.Add(friend.To);
                var occupiedCopy = occupied | friend.To.Possibilities;

                if (CheckForChain(solverData, chain, linkGraph)) return true;
                if(Search(solverData, graph, occupiedCopy, explored, chain, linkGraph)) return true;
                
                chain.RemoveLast();
            }
        }

        return false;
    }

    private bool CheckForLoop(ISudokuSolverData solverData, ChainBuilder<IPossibilitiesPositions, Cell> builder,
        Cell[] possibleLastLinks)
    {
        foreach (var ll in possibleLastLinks)
        {
            if (ll == builder.FirstLink()) continue;
            var chain = builder.ToChain();

            foreach (var pp in chain.Elements)
            {
                foreach (var cell in pp.EachCell())
                {
                    foreach (var p in solverData.PossibilitiesAt(cell).EnumeratePossibilities())
                    {
                        var cp = new CellPossibility(cell, p);
                        if (!Contains(chain, cp)) solverData.ChangeBuffer.ProposePossibilityRemoval(cp);
                    }
                }
            }

            return solverData.ChangeBuffer.NotEmpty() && solverData.ChangeBuffer.Commit(
                new AlmostHiddenSetsChainReportBuilder(chain, ll)) && StopOnFirstPush;
        }

        return false;
    }

    private bool Contains(Chain<IPossibilitiesPositions, Cell> chain, CellPossibility cp)
    {
        foreach (var element in chain)
        {
            if (element.Contains(cp)) return true;
        }

        return false;
    }

    private bool CheckForChain(ISudokuSolverData solverData, ChainBuilder<IPossibilitiesPositions, Cell> chain, ILinkGraph<CellPossibility> linkGraph)
    {
        if (!_checkLength2 && chain.Count == 2) return false;
        
        List<Link<CellPossibility>> links = new();

        var first = chain.FirstElement();
        var last = chain.LastElement();

        var nope = new GridPositions();
        nope.Add(chain.FirstLink());
        nope.Add(chain.LastLink());

        foreach (var cell in first.EachCell())
        {
            if (nope.Contains(cell)) continue;
            
            foreach (var possibility in solverData.PossibilitiesAt(cell).EnumeratePossibilities())
            {
                if (first.Possibilities.Contains(possibility) || last.Possibilities.Contains(possibility)) continue;

                var current = new CellPossibility(cell, possibility);
                foreach (var friend in linkGraph.Neighbors(current, LinkStrength.Strong))
                {
                    if (nope.Contains(friend.ToCell())) continue;
                    
                    foreach (var friendOfFriend in linkGraph.Neighbors(friend, LinkStrength.Strong))
                    {
                        var asCell = friendOfFriend.ToCell();

                        if (last.Positions.Contains(friendOfFriend.ToCell()) && asCell != cell && !nope.Contains(asCell))
                        {
                            links.Add(new Link<CellPossibility>(current, friend));
                            links.Add(new Link<CellPossibility>(friend, friendOfFriend));

                            solverData.ChangeBuffer.ProposeSolutionAddition(friend);
                        }
                    }
                }
            }
        }
        
        return solverData.ChangeBuffer.NotEmpty() && solverData.ChangeBuffer.Commit(
            new AlmostHiddenSetsChainReportBuilder(chain.ToChain(), links)) && StopOnFirstPush;
    }
}

public class AlmostHiddenSetsChainReportBuilder : IChangeReportBuilder<NumericChange, ISudokuSolvingState, ISudokuHighlighter>
{
    private readonly Chain<IPossibilitiesPositions, Cell> _chain;
    private readonly List<Link<CellPossibility>> _links;
    private readonly Cell? _additionalLink;

    public AlmostHiddenSetsChainReportBuilder(Chain<IPossibilitiesPositions, Cell> chain, List<Link<CellPossibility>> links)
    {
        _chain = chain;
        _links = links;
        _additionalLink = null;
    }

    public AlmostHiddenSetsChainReportBuilder(Chain<IPossibilitiesPositions, Cell> chain, Cell additionalLink)
    {
        _chain = chain;
        _links = new List<Link<CellPossibility>>();
        _additionalLink = additionalLink;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<NumericChange> changes, ISudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>( _chain.ToString(), lighter =>
        {
            var color = (int)ChangeColoration.CauseOffOne;
            foreach (var ahs in _chain.Elements)
            {
                foreach (var possibility in ahs.Possibilities.EnumeratePossibilities())
                {
                    foreach (var cell in ahs.EachCell(possibility))
                    {
                        lighter.HighlightPossibility(possibility, cell.Row, cell.Column, (ChangeColoration)color);
                    }
                }

                color++;
            }

            foreach (var cell in _chain.Links)
            {
                lighter.EncircleCell(cell);
            }
            if(_additionalLink is not null) lighter.EncircleCell(_additionalLink.Value);

            foreach (var link in _links)
            {
                lighter.CreateLink(link.From, link.To, LinkStrength.Strong);
            }

            ChangeReportHelper.HighlightChanges(lighter, changes);
        });
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<NumericChange> changes, ISudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}