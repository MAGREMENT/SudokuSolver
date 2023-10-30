﻿using System.Windows;
using System.Windows.Controls;
using Model;
using Model.Solver;
using Model.Solver.Helpers.Changes;
using SudokuSolver.Utils;

namespace SudokuSolver;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IGraphicsManager
    {
        public event OnTranslationTypeChanged? TranslationTypeChanged;
        
        private bool _createNewSudoku = true;

        public MainWindow()
        {
            InitializeComponent();

            var unused = new SolverStateManager(this, Solver, LogList);

            Solver.IsReady += () =>
            {
                SolveButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
            };
            Solver.CellClickedOn += (sender, row, col) =>
            {
                LiveModification.SetCurrent(sender, row, col);
            };
            Solver.SudokuAsStringChanged += ShowSudokuAsString;
            Solver.LogsUpdated += logs =>
            {
                LogList.Dispatcher.Invoke(() => LogList.InitLogs(logs));
                ExplanationBox.Dispatcher.Invoke(() => ExplanationBox.Text = "");
            };
            Solver.CurrentStrategyChanged += index =>
                StrategyList.Dispatcher.Invoke(() => StrategyList.HighlightStrategy(index));

            LiveModification.LiveModified += (number, row, col, action) =>
            {
                if (action == ChangeType.Solution) Solver.AddDefinitiveNumber(number, row, col);
                else if(action == ChangeType.Possibility) Solver.RemovePossibility(number, row, col);
            };
            
            StrategyList.InitStrategies(Solver.GetStrategies());
            StrategyList.StrategyExcluded += Solver.ExcludeStrategy;
            StrategyList.StrategyUsed += Solver.UseStrategy;

            DelayBeforeSlider.Value = Solver.DelayBefore;
            DelayAfterSlider.Value = Solver.DelayAfter;
        }
        
        public void ShowSudokuAsString(string asString)
        {
            _createNewSudoku = false;
            SudokuStringBox.Text = asString;
            _createNewSudoku = true;
        }

        public void ShowExplanation(string explanation)
        {
            ExplanationBox.Text = explanation;
        }

        private void NewSudoku(object sender, TextChangedEventArgs e)
        {
            if (_createNewSudoku) Solver.NewSudoku(SudokuTranslator.Translate(SudokuStringBox.Text));
        }

        private void SolveSudoku(object sender, RoutedEventArgs e)
        {
            SolveButton.IsEnabled = false;
            ClearButton.IsEnabled = false;

            SolverUserControl suc = Solver;
            
            if (StepByStepOption.IsChecked == true) suc.RunUntilProgress();
            else suc.SolveSudoku();
        }

        private void ClearSudoku(object sender, RoutedEventArgs e)
        {
            SolverUserControl suc = Solver;
            suc.ClearSudoku();
        }
        
        private void SelectedTranslationType(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox box || Solver is null) return;
            Solver.TranslationType = (SudokuTranslationType) box.SelectedIndex;
            TranslationTypeChanged?.Invoke();
        }
        
        private void SelectedOnInstanceFound(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox box || Solver is null) return;
            Solver.SetOnInstanceFound((OnInstanceFound) box.SelectedIndex);
        }
        
        private void SetSolverDelayBefore(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is not Slider slider) return;
            Solver.DelayBefore = (int)slider.Value;
        }
        
        private void SetSolverDelayAfter(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is not Slider slider) return;
            Solver.DelayAfter = (int)slider.Value;
        }

        private void AllowUniqueness(object sender, RoutedEventArgs e)
        {
            if (Solver is null) return;
            Solver.AllowUniqueness(true);
            StrategyList.UpdateStrategies(Solver.GetStrategies());
        }

        private void ForbidUniqueness(object sender, RoutedEventArgs e)
        {
            Solver.AllowUniqueness(false);
            StrategyList.UpdateStrategies(Solver.GetStrategies());
        }
    }
