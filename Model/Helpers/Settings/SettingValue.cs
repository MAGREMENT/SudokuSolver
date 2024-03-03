﻿using System;
using Model.Utility;

namespace Model.Helpers.Settings;

public abstract class SettingValue
{
    public virtual bool ToBool()
    {
        return default;
    }

    public virtual int ToInt()
    {
        return default;
    }

    public virtual MinMax ToMinMax()
    {
        return default;
    }
    
    protected static bool TranslateBoolean(string s)
    {
        return s.ToLower() == "true";
    }

    protected static int TranslateInt(string s)
    {
        try
        {
            return int.Parse(s);
        }
        catch (Exception)
        {
            return default;
        }
    }

    protected static MinMax TranslateMinMax(string s)
    {
        var split = s.Split(',');
        if (split.Length != 2) return default;

        try
        {
            return new MinMax(int.Parse(split[0]), int.Parse(split[1]));
        }
        catch (Exception)
        {
            return default;
        }
    }
}

public class StringSettingValue : SettingValue
{
    private readonly string _s;

    public StringSettingValue(string s)
    {
        _s = s;
    }

    public override bool ToBool()
    {
        return TranslateBoolean(_s);
    }

    public override int ToInt()
    {
        return TranslateInt(_s);
    }

    public override MinMax ToMinMax()
    {
        return TranslateMinMax(_s);
    }

    public override string ToString()
    {
        return _s;
    }
}

public class IntSettingValue : SettingValue
{
    private readonly int _i;

    public IntSettingValue(int i)
    {
        _i = i;
    }

    public IntSettingValue(string s)
    {
        _i = TranslateInt(s);
    }

    public override int ToInt()
    {
        return _i;
    }

    public override string ToString()
    {
        return _i.ToString();
    }
}

public class BoolSettingValue : SettingValue
{
    private readonly bool _b;

    public BoolSettingValue(bool b)
    {
        _b = b;
    }

    public BoolSettingValue(string s)
    {
        _b = TranslateBoolean(s);
    }

    public override bool ToBool()
    {
        return _b;
    }

    public override string ToString()
    {
        return _b.ToString();
    }
}

public class MinMaxSettingValue : SettingValue
{
    private readonly MinMax _minMax;

    public MinMaxSettingValue(MinMax minMax)
    {
        _minMax = minMax;
    }

    public MinMaxSettingValue(string s)
    {
        TranslateMinMax(s);
    }

    public override MinMax ToMinMax()
    {
        return _minMax;
    }

    public override string ToString()
    {
        return _minMax.ToString();
    }
}