﻿using Global.Enums;

namespace Model.Player;

public struct PlayerCell
{
    /*
     * 0-8 bottom possibilities
     * 9-17 possibilities
     * 18-26 top possibilities
     * 27-31 number
     */
    private int _bits;

    public PlayerCell()
    {
        _bits = 0;
    }

    public bool IsNumber()
    {
        return ((_bits >> 27) & 0xF) > 0;
    }

    public int Number()
    {
        return (_bits >> 27) & 0xF;
    }

    public void SetNumber(int n)
    {
        _bits = n << 27;
    }

    public int[] Possibilities(PossibilitiesLocation location)
    {
        var buffer = (_bits >> (int)location) & 0x7F;
        var result = new int[System.Numerics.BitOperations.PopCount((uint)buffer)];
        var cursor = 0;
        for (int i = 0; i < 8 || cursor == result.Length; i++)
        {
            if (Peek(buffer, i)) result[cursor++] = i + 1;
        }

        return result;
    }

    private bool AddPossibility(int possibility, PossibilitiesLocation location)
    {
        var delta = possibility - 1 + (int)location;
        var yes = !Peek(_bits, delta);
        Set(delta);
        return yes;
    }
    
    private bool RemovePossibility(int possibility, PossibilitiesLocation location)
    {
        var delta = possibility - 1 + (int)location;
        var yes = Peek(_bits, delta);
        UnSet(possibility - 1 + (int)location);
        return yes;
    }


    private static bool Peek(int bits, int n)
    {
        return ((bits >> n) & 1) > 0;
    }

    private void Set(int n)
    {
        _bits |= 1 << n;
    }

    private void UnSet(int n)
    {
        _bits &= ~(1 << n);
    }
}