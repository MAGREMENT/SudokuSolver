﻿using System.Windows;
using System.Windows.Controls;
using DesktopApplication.Presenter.Sudokus.Solve;
using DesktopApplication.Presenter.Sudokus.Solve.ChooseStep;
using Model;
using Model.Core;

namespace DesktopApplication.View.HelperWindows;

public partial class ChooseStepWindow : IChooseStepView
{
    private readonly ChooseStepPresenter _presenter;
    
    public ChooseStepWindow(ChooseStepPresenterBuilder builder)
    {
        InitializeComponent();
        _presenter = builder.Build(this);
        
        _presenter.Initialize();
        
        TitleBar.RefreshMaximizeRestoreButton(WindowState);
        StateChanged += (_, _) => TitleBar.RefreshMaximizeRestoreButton(WindowState);
    }
    
    private void Minimize()
    {
        WindowState = WindowState.Minimized;
    }

    private void ChangeSize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    public ISudokuSolverDrawer Drawer => Board;
    
    public void ClearCommits()
    {
        StepsPanel.Children.Clear();
    }

    public void AddCommit(Strategy maker, int index)
    {
        var tb = new TextBlock
        {
            Text = maker.Name,
            Style = (Style)FindResource("SearchResult")
        };
        tb.SetResourceReference(ForegroundProperty, ThemeInformation.ResourceNameFor(maker.Difficulty));
        
        tb.MouseLeftButtonDown += (_, _) => _presenter.ShowStep(index);
        
        StepsPanel.Children.Add(tb);
    }

    public void SetPreviousPageExistence(bool exists)
    {
        PreviousButton.IsEnabled = exists;
    }

    public void SetNextPageExistence(bool exists)
    {
        NextButton.IsEnabled = exists;
    }

    public void SetTotalPage(int n)
    {
        TotalPage.Text = n.ToString();
    }

    public void SetCurrentPage(int n)
    {
        CurrentPage.Text = n.ToString();
    }

    public void SelectStep(int index)
    {
        if (index < 0 || index >= StepsPanel.Children.Count || StepsPanel.Children[index] is not TextBlock tb) return;

        tb.FontSize = 25;
        tb.FontWeight = FontWeights.SemiBold;
    }

    public void UnselectStep(int index)
    {
        if (index < 0 || index >= StepsPanel.Children.Count || StepsPanel.Children[index] is not TextBlock tb) return;

        tb.FontSize = 16;
        tb.FontWeight = FontWeights.Normal;
    }

    public void EnableSelection(bool isEnabled)
    {
        SelectButton.IsEnabled = isEnabled;
    }

    private void PreviousPage(object sender, RoutedEventArgs e)
    {
        _presenter.ChangePage(-1);
    }

    private void NextPage(object sender, RoutedEventArgs e)
    {
        _presenter.ChangePage(1);
    }

    private void OnSelection(object sender, RoutedEventArgs e)
    {
        _presenter.SelectCurrent();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}