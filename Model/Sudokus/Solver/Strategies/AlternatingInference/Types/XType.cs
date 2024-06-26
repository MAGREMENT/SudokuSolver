﻿using Model.Core;
using Model.Sudokus.Solver.Utility;
using Model.Sudokus.Solver.Utility.Graphs;
using Model.Utility;

namespace Model.Sudokus.Solver.Strategies.AlternatingInference.Types;

public class XType : IAlternatingInferenceType<CellPossibility>
{
    public const string OfficialLoopName = "X-Cycles";
    public const string OfficialChainName = "X-Chains";
    
    public string LoopName => OfficialLoopName;
    public string ChainName => OfficialChainName;
    public StepDifficulty Difficulty => StepDifficulty.Hard;
    public SudokuStrategy? Strategy { get; set; }
    public ILinkGraph<CellPossibility> GetGraph(ISudokuSolverData solverData)
    {
        solverData.PreComputer.Graphs.ConstructSimple(SudokuConstructRuleBank.UnitStrongLink, SudokuConstructRuleBank.UnitWeakLink);
        return solverData.PreComputer.Graphs.SimpleLinkGraph;
    }

    public bool ProcessFullLoop(ISudokuSolverData solverData, LinkGraphLoop<CellPossibility> loop)
    {
        loop.ForEachLink((one, two)
            => ProcessWeakLink(solverData, one, two), LinkStrength.Weak);

        return solverData.ChangeBuffer.Commit(
            new AlternatingInferenceLoopReportBuilder<CellPossibility>(loop, LoopType.NiceLoop));
    }

    private void ProcessWeakLink(ISudokuSolverData view, CellPossibility one, CellPossibility two)
    {
        foreach (var coord in one.SharedSeenCells(two))
        {
            view.ChangeBuffer.ProposePossibilityRemoval(one.Possibility, coord.Row, coord.Column);
        }
    }

    public bool ProcessWeakInferenceLoop(ISudokuSolverData solverData, CellPossibility inference, LinkGraphLoop<CellPossibility> loop)
    {
        solverData.ChangeBuffer.ProposePossibilityRemoval(inference.Possibility, inference.Row, inference.Column);
        return solverData.ChangeBuffer.Commit(
            new AlternatingInferenceLoopReportBuilder<CellPossibility>(loop, LoopType.WeakInference));
    }

    public bool ProcessStrongInferenceLoop(ISudokuSolverData solverData, CellPossibility inference, LinkGraphLoop<CellPossibility> loop)
    {
        solverData.ChangeBuffer.ProposeSolutionAddition(inference.Possibility, inference.Row, inference.Column);
        return solverData.ChangeBuffer.Commit(
            new AlternatingInferenceLoopReportBuilder<CellPossibility>(loop, LoopType.StrongInference));
    }

    public bool ProcessChain(ISudokuSolverData solverData, LinkGraphChain<CellPossibility> chain, ILinkGraph<CellPossibility> graph)
    {
        return IAlternatingInferenceType<CellPossibility>.ProcessChainWithSimpleGraph(solverData,
            chain, graph, Strategy!);
    }
}