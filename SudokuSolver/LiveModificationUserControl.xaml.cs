﻿using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SudokuSolver;

public partial class LiveModificationUserControl : UserControl
{
    private const int FullFontSize = 100;
    
    private readonly TextBlock _text;
    private readonly RadioButton _definitiveNumber;
    private readonly RadioButton _possibilities;

    private SudokuCellUserControl? _current = null;
    private int[] _currentPos = new int[2];

    private SudokuUserControl? _solverAccess = null;

    public LiveModificationUserControl()
    {
        InitializeComponent();

        _text = (FindName("Text") as TextBlock)!;
        _text.Focusable = true;
        _text.Background = new SolidColorBrush(Colors.Honeydew);
        _text.LostFocus += (_, _) =>
        {
            _text.Background = new SolidColorBrush(Colors.Honeydew);
        };
        _text.GotFocus += (_, _) =>
        {
            _text.Background = new SolidColorBrush(Colors.Lavender);
        };
        _text.MouseDown += (_, _) =>
        {
            _text.Focus();
        };

        _definitiveNumber = (FindName("A") as RadioButton)!;
        _possibilities = (FindName("B") as RadioButton)!;
    }

    public void Init(SudokuUserControl suc)
    {
        _solverAccess = suc;
    }

    public void SetCurrent(SudokuCellUserControl scuc, int row, int col)
    {
        if (_current is not null) _current.Updated -= Update;
        _current = scuc;
        _currentPos[0] = row;
        _currentPos[1] = col;

        _current.Updated += Update;
        Update();
    }

    private void Update()
    {
        if (_current is not null)
        {
            _text.FontSize = _current.IsPossibilities ? FullFontSize / 4 : FullFontSize;
            _text.Text = _current.Text;
        
            _text.Focus();
        }
    }

    private void KeyPressed(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.NumPad1 : LiveModification(1);
                break;
            case Key.NumPad2 : LiveModification(2);
                break;
            case Key.NumPad3 : LiveModification(3);
                break;
            case Key.NumPad4 : LiveModification(4);
                break;
            case Key.NumPad5 : LiveModification(5);
                break;
            case Key.NumPad6 : LiveModification(6);
                break;
            case Key.NumPad7 : LiveModification(7);
                break;
            case Key.NumPad8 : LiveModification(8);
                break;
            case Key.NumPad9 : LiveModification(9);
                break;
        }
    }

    private void LiveModification(int i)
    {
        if (_solverAccess is not null && _current is not null)
        {
            if (_definitiveNumber.IsChecked == true)
                _solverAccess.AddDefinitiveNumber(i, _currentPos[0], _currentPos[1]);
            if (_possibilities.IsChecked == true)
                _solverAccess.RemovePossibility(i, _currentPos[0], _currentPos[1]); 
        }
    }
}