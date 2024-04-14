﻿using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopApplication.View.Sudoku.Controls;

public partial class FilledSudokuGeneratorControl : UserControl
{
    public FilledSudokuGeneratorControl()
    {
        InitializeComponent();
    }
    
    public void Activate(bool activated)
    {
        ActivatedLamp.Background = activated ? Brushes.ForestGreen : Brushes.Red;
    }
}