﻿namespace Model.Helpers.Settings.Types;

public class BooleanSetting : ISetting
{
    public string Name { get; }
    public ISettingViewInterface Interface { get; } = new BooleanViewInterface();
    public bool Value { get; set; }

    public BooleanSetting(string name, bool defaultValue = false)
    {
        Name = name;
        Value = defaultValue;
    }
    
    public SettingValue Get()
    {
        return new BoolSettingValue(Value);
    }
    
    public void Set(SettingValue s)
    {
        Value = s.ToBool();
    }
}