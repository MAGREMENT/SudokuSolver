﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using DesktopApplication.Presenter.Sudoku;
using DesktopApplication.Presenter.Sudoku.Generate;
using DesktopApplication.View.Controls;
using DesktopApplication.View.HelperWindows;
using DesktopApplication.View.Sudoku.Controls;
using Model.Sudoku.Generator;

namespace DesktopApplication.View.Sudoku.Pages;

public partial class GeneratePage : ISudokuGenerateView
{
    private readonly SudokuGeneratePresenter _presenter;
    private readonly bool _initialized;
    
    public GeneratePage(SudokuApplicationPresenter appPresenter)
    {
        InitializeComponent();
        _presenter = appPresenter.Initialize(this);

        RenderOptions.SetBitmapScalingMode(Bin, BitmapScalingMode.Fant);

        _initialized = true;
    }

    public override void OnShow()
    {
        
    }

    public override void OnClose()
    {
       
    }

    public override object TitleBarContent()
    {
        var settings = new SettingsButton();
        settings.Clicked += () =>
        {
            var window = new SettingWindow(_presenter.SettingsPresenter);
            window.Show();
        };

        return settings;
    }

    public void ActivateFilledSudokuGenerator(bool activated)
    {
        FSG.Dispatcher.Invoke(() => FSG.Activate(activated));
    }

    public void ActivateRandomDigitRemover(bool activated)
    {
        RDR.Dispatcher.Invoke(() => RDR.Activate(activated));
    }

    public void ActivatePuzzleEvaluator(bool activated)
    {
        Evaluator.Dispatcher.Invoke(() => Evaluator.Activate(activated));
    }

    public async void ShowTransition(TransitionPlace place)
    {
        var path = place switch
        {
            TransitionPlace.ToEvaluator => ToEvaluator,
            TransitionPlace.ToFinalList => ToFinalList,
            TransitionPlace.ToRDR => ToRDR,
            TransitionPlace.ToBin => ToBin,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        path.Dispatcher.Invoke(() => path.SetResourceReference(Shape.StrokeProperty, "Primary1"));
        await Task.Delay(TimeSpan.FromMilliseconds(250));
        path.Dispatcher.Invoke(() => path.SetResourceReference(Shape.StrokeProperty, "Text"));
    }

    public void UpdateEvaluatedList(IEnumerable<GeneratedSudokuPuzzle> sudokus)
    {
        Evaluated.Dispatcher.Invoke(() =>
        {
            Evaluated.Children.Clear();

            foreach (var sudoku in sudokus)
            {
                Evaluated.Children.Add(new GeneratedPuzzleControl(sudoku));
            }
        });
    }

    public void AllowGeneration(bool allowed)
    {
        GenerateButton.IsEnabled = allowed;
    }

    public void AllowCancel(bool allowed)
    {
        StopButton.Dispatcher.Invoke(() => StopButton.IsEnabled = allowed);
    }

    public void SetCriteriaList(IReadOnlyList<EvaluationCriteria> criteriaList)
    {
        Evaluator.SetCriteriaList(criteriaList);
    }

    private void Generate(object sender, RoutedEventArgs e)
    {
        _presenter.Generate();
    }

    private void OnValueChange(int value)
    {
        if(_initialized) _presenter.SetGenerationCount(value);
    }

    private void ChangeSymmetry(bool value)
    {
        _presenter.SetKeepSymmetry(value);
    }

    private void OpenManageCriteriaWindow()
    {
        var window = new ManageCriteriaWindow(_presenter.ManageCriteria());
        window.Show();
    }

    private void Stop(object sender, RoutedEventArgs e)
    {
        _presenter.Stop();
    }
}