﻿using System;
using Model.Utility;

namespace Model.Helpers.Settings.Types;

public class EnumSetting<TEnum> : ISetting where TEnum : struct, Enum
{
    public string Name { get; }
    public ISettingInteractionInterface InteractionInterface { get; }
    public TEnum Value { get; set; }

    public EnumSetting(string name, IStringConverter? converter, TEnum defaultValue)
    {
        Name = name;
        InteractionInterface = new EnumListInteractionInterface<TEnum>(converter);
        Value = defaultValue;
    }
    
    public SettingValue Get()
    {
        return new IntSettingValue(EnumConverter.ToInt(Value));
    }

    public void Set(SettingValue s)
    {
        Value = EnumConverter.ToEnum<TEnum>(s.ToInt());
    }
}

