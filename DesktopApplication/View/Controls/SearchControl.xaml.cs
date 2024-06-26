﻿using System.Windows;
using System.Windows.Controls;

namespace DesktopApplication.View.Controls;

public partial class SearchControl
{
    private bool _hasBoxContent;
    private bool _raiseEvent = true;

    public event OnSearch? Searched;
    
    public SearchControl()
    {
        InitializeComponent();

        Box.Focusable = true;
        Box.GotFocus += (_, _) =>
        {
            _raiseEvent = false;
            
            if (!_hasBoxContent) Box.Text = "";

            _raiseEvent = true;
        };

        Box.LostFocus += (_, _) =>
        {
            _raiseEvent = false;

            if (string.IsNullOrWhiteSpace(Box.Text))
            {
                Box.Text = "Search";
                _hasBoxContent = false;
            }
            else _hasBoxContent = true;
            
            _raiseEvent = true;
        };
    }

    private void Box_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if(_raiseEvent) Searched?.Invoke(Box.Text);
    }

    public void ClearResult()
    {
        ResultPanel.Children.Clear();
    }

    public void AddResult(UIElement element)
    {
        ResultPanel.Children.Add(element);
    }
}

public delegate void OnSearch(string s);
