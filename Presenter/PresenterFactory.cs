﻿using Model;
using Model.Solver;
using Model.Solver.Helpers.Changes;
using Presenter.Solver;
using Presenter.StepChooser;
using Presenter.StrategyManager;
using Repository;

namespace Presenter;

public class PresenterFactory
{
    private readonly ISolver _solver;

    public PresenterFactory()
    {
        var repository = new JSONRepository<List<StrategyDAO>>("strategies.json");
        try
        {
            repository.Initialize();
        }
        catch (RepositoryInitializationException)
        {
            repository.New(new List<StrategyDAO>());
        }
        
        
        _solver = new SudokuSolver(repository)
        {
            StatisticsTracked = false,
            LogsManaged = true
        };
    }

    public SolverPresenter Create(ISolverView view)
    {
        return new SolverPresenter(_solver, view);
    }

    public StrategyManagerPresenter Create(IStrategyManagerView view)
    {
        return new StrategyManagerPresenter(_solver.StrategyLoader, view);
    }
}

public class StepChooserPresenterBuilder
{
    private readonly SolverState _state;
    private readonly BuiltChangeCommit[] _commits;
    private readonly IStepChooserCallback _callback;

    public StepChooserPresenterBuilder(SolverState state, BuiltChangeCommit[] commits, IStepChooserCallback callback)
    {
        _commits = commits;
        _callback = callback;
        _state = state;
    }

    public StepChooserPresenter Build(IStepChooserView view)
    {
        return new StepChooserPresenter(_state, _commits, view, _callback);
    }
}