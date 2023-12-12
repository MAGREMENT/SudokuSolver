﻿using System.Windows.Input;
using Presenter;
using View.Pages;
using View.Pages.Player;
using View.Pages.Solver;
using View.Pages.StrategyManager;

namespace View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : IPageHandler
{
    private readonly HandledPage[] _pages;
    private HandledPage? _currentlyShown;

    private bool _navigationAllowed = false;
    
    public MainWindow()
    {
        InitializeComponent();

        var factory = new PresenterFactory();
        
        _pages = new HandledPage[]
        {
            new FirstPage(this), new SolverPage(this, factory), new PlayerPage(this, factory), new StrategyManagerPage(this, factory)
        };

        Main.NavigationService.Navigating += (_, args) =>
        {
            if (!_navigationAllowed) args.Cancel = true;
        };

        ShowPage(PagesName.First);
    }

    public void ShowPage(PagesName pageName)
    {
        _navigationAllowed = true;
        _currentlyShown?.OnQuit();

        var page = _pages[(int)pageName];
        Main.Content = page;
        _currentlyShown = page;
        
        _currentlyShown.OnShow();
        _navigationAllowed = false;
    }
}
