﻿using Model;
using Model.Player;
using Model.Solver;
using Model.Solver.Helpers.Changes;
using Presenter.Player;
using Presenter.Solver;
using Presenter.StepChooser;
using Presenter.StrategyManager;
using Presenter.Translators;
using Repository;

namespace Presenter;

public class ApplicationPresenter
{
    private readonly IViewManager _manager;
    
    private readonly Settings _settings;
    private readonly ISolver _solver;
    private readonly IPlayer _player;

    private readonly ViewTheme[] _themes;

    private ApplicationPresenter(IViewManager manager)
    {
        _manager = manager;
        
        //Solver
        var solver = new SudokuSolver
        {
            StatisticsTracked = false,
            LogsManaged = true
        };
        IRepository<List<StrategyDAO>> strategyRepository = new JSONRepository<List<StrategyDAO>>("strategies.json"); //TODO to array ?
        strategyRepository.InitializeOrDefault(new List<StrategyDAO>());
        solver.Bind(strategyRepository);
        _solver = solver;

        //Player
        _player = new SudokuPlayer();

        //Settings
        _settings = new Settings();
        IRepository<SettingsDAO> settingsRepository = new JSONRepository<SettingsDAO>("settings.json");
        settingsRepository.InitializeOrDefault(_settings.ToDAO());
        _settings.Bind(settingsRepository);

        //Themes
        IRepository<ThemeDAO[]> themeRepository = new HardCodedThemeRepository();
        themeRepository.InitializeOrDefault(Array.Empty<ThemeDAO>());
        var download = themeRepository.Download();
        _themes = download is null ? Array.Empty<ViewTheme>() : ViewTheme.From(download);
        _settings.ThemeChanged += () => _manager.ApplyTheme(_themes[_settings.Theme]);
    }

    public static ApplicationPresenter Initialize(IViewManager manager)
    {
        return new ApplicationPresenter(manager);
    }

    public void ViewInitializationFinished()
    {
        if(_themes.Length > 0) _manager.ApplyTheme(_themes[_settings.Theme]);
    }

    public SolverPresenter Create(ISolverView view)
    {
        return new SolverPresenter(_solver, view, _settings);
    }

    public StrategyManagerPresenter Create(IStrategyManagerView view)
    {
        return new StrategyManagerPresenter(_solver.StrategyLoader, view);
    }

    public PlayerPresenter Create(IPlayerView view)
    {
        return new PlayerPresenter(_player, view, _settings);
    }

    public IGlobalSettings GlobalSettings => _settings;
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