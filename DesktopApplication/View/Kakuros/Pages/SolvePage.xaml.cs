﻿using DesktopApplication.Presenter.Kakuros;
using DesktopApplication.Presenter.Kakuros.Solve;
using DesktopApplication.View.Kakuros.Controls;

namespace DesktopApplication.View.Kakuros.Pages;

public partial class SolvePage : IKakuroSolveView
{
    private readonly KakuroSolvePresenter _presenter;

    public IKakuroSolverDrawer Drawer => (KakuroBoard)ContentControl.OptimizableContent!;
    
    public SolvePage(KakuroApplicationPresenter presenter)
    {
        InitializeComponent();

        _presenter = presenter.Initialize(this);
    }


    private void OnNewKakuro(string s)
    {
        _presenter.SetNewKakuro(s);
    }
}