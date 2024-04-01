﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using Model.Helpers.Settings;
using Model.Helpers.Settings.Types;
using Model.Sudoku.Solver;
using Model.Utility;

namespace DesktopApplication.Presenter.Sudoku.Manage;

public class StrategySettingsPresenter : IEnumerable<(ISetting, int)>, ISettingCollection
{
    private readonly SudokuStrategy _strategy;
    private readonly IStrategyRepositoryUpdater _updater;

    public StrategySettingsPresenter(SudokuStrategy strategy, IStrategyRepositoryUpdater updater)
    {
        _strategy = strategy;
        _updater = updater;
    }

    public IEnumerator<(ISetting, int)> GetEnumerator()
    {
        yield return (new BooleanSetting("Enabled", _strategy.Enabled), -1);
        yield return (new EnumSetting<InstanceHandling>("Instance handling",SpaceConverter.Instance, _strategy.InstanceHandling), -2);
        for (int i = 0; i < _strategy.Settings.Count; i++)
        {
            yield return (_strategy.Settings[i], i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Set(int index, SettingValue value)
    {
        switch (index)
        {
            case -1:
                _strategy.Enabled = value.ToBool();
                break;
            case -2:
                _strategy.InstanceHandling = (InstanceHandling)value.ToInt();
                break;
            default:
                _strategy.Set(index, value);
                break;
        }

        _updater.Update();
    }
}

