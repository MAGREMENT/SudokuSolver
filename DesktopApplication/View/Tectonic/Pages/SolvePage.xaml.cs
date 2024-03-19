﻿using System.Windows;
using DesktopApplication.Presenter.Tectonic;
using DesktopApplication.Presenter.Tectonic.Solve;
using DesktopApplication.View.Controls;
using Model;
using Model.Helpers.Highlighting;
using Model.Helpers.Logs;

namespace DesktopApplication.View.Tectonic.Pages;

public partial class SolvePage : ITectonicSolveView
{
    private readonly TectonicSolvePresenter _presenter;

    private int _logOpen;
    
    public SolvePage(TectonicApplicationPresenter appPresenter)
    {
        InitializeComponent();

        _presenter = appPresenter.Initialize(this);
    }

    public ITectonicDrawer Drawer => EmbeddedDrawer.Drawer;

    public void SetTectonicString(string s)
    {
        TextBox.SetText(s);
    }

    public void AddLog(ISolverLog<ITectonicHighlighter> log, StateShown stateShown)
    {
        LogPanel.Dispatcher.Invoke(() =>
        {
            var lc = new LogControl(log, stateShown);
            LogPanel.Children.Add(lc);
            lc.OpenRequested += _presenter.RequestLogOpening;
            lc.StateShownChanged += _presenter.RequestStateShownChange;
            lc.HighlightShifted += _presenter.RequestHighlightShift;
        });
        LogViewer.Dispatcher.Invoke(() => LogViewer.ScrollToEnd());
    }

    public void ClearLogs()
    {
        LogPanel.Children.Clear();
    }

    public void OpenLog(int index)
    {
        if (index < 0 || index > LogPanel.Children.Count) return;
        if (LogPanel.Children[index] is not LogControl lc) return;

        _logOpen = index;
        lc.Open();
    }

    public void CloseLogs()
    {
        if (_logOpen < 0 || _logOpen > LogPanel.Children.Count) return;
        if (LogPanel.Children[_logOpen] is not LogControl lc) return;

        _logOpen = -1;
        lc.Close();
    }

    public void SetLogsStateShown(StateShown stateShown)
    {
        foreach (var child in LogPanel.Children)
        {
            if (child is not LogControl lc) continue;

            lc.SetStateShown(stateShown);
        }
    }

    public void SetCursorPosition(int index, string s)
    {
        if (index < 0 || index > LogPanel.Children.Count) return;
        if (LogPanel.Children[index] is not LogControl lc) return;

        lc.SetCursorPosition(s);
    }

    private void CreateNewTectonic(string s)
    {
        _presenter.SetNewTectonic(s);
    }

    private void Solve(object sender, RoutedEventArgs e)
    {
        _presenter.Solve(false);
    }

    private void Advance(object sender, RoutedEventArgs e)
    {
        _presenter.Solve(true);
    }

    private void ChooseStep(object sender, RoutedEventArgs e)
    {
        
    }

    private void Clear(object sender, RoutedEventArgs e)
    {
        
    }

    private void EmbeddedDrawer_OnRowCountChanged(int number)
    {
        RowCount.SetDimension(number);
    }

    private void EmbeddedDrawer_OnColumnCountChanged(int number)
    {
        ColumnCount.SetDimension(number);
    }

    private void RowCount_OnDimensionChangeAsked(int diff)
    {
        _presenter.SetNewRowCount(diff);
    }

    private void ColumnCount_OnDimensionChangeAsked(int diff)
    {
        _presenter.SetNewColumnCount(diff);
    }

    private void OnHideableTextboxShowed()
    {
        _presenter.SetTectonicString();
    }
}