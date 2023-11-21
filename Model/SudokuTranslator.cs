﻿using System;
using System.Text;
using Global.Enums;
using Model.Solver.Possibility;
using Model.Utility;

namespace Model;

public static class SudokuTranslator
{
    public static string LineTranslate(ITranslatable translatable, SudokuTranslationType type)
    {
        string result = "";
        int voidCount = 0;
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                int current = translatable[i, j];
                if (current == 0)
                {
                    switch (type)
                    {
                        case SudokuTranslationType.Shortcuts :
                            voidCount++;
                            break;
                        case SudokuTranslationType.Zeros :
                            result += "0";
                            break;
                        case SudokuTranslationType.Points :
                            result += ".";
                            break;
                    }
                }
                else
                {
                    if (voidCount != 0)
                    {
                        result += voidCount > 3 ? "s" + voidCount + "s" : StringUtility.Repeat(" ", voidCount);
                        voidCount = 0;
                    }

                    result += current;
                }
            }
        }

        return result;
    }
    
    public static Sudoku LineTranslate(string asString)
    {
        Sudoku s = new();
        int n = 0;
        bool isCounting = false;
        string buffer = "";

        try
        {
            foreach (var c in asString)
            {
                switch (c)
                {
                    case 's' when isCounting:
                    {
                        n += int.Parse(buffer);
                        buffer = "";
                        isCounting = false;
                        break;
                    }
                    case 's':
                        isCounting = true;
                        break;
                    case ' ': case '.' :
                        s[n / 9, n % 9] = 0;

                        n++;
                        break;
                    default:
                    {
                        if (isCounting) buffer += c;
                        else
                        {
                            s[n / 9, n % 9] = int.Parse(c.ToString());
                            n++;
                        }

                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            return new Sudoku();
        }

        return s;
    }

    public static string GridTranslate(ITranslatable translatable)
    {
        var maxWidth = 0;
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var width = translatable[row, col] == 0 ? translatable.PossibilitiesAt(row, col).Count : 1;
                maxWidth = Math.Max(width, maxWidth);
            }
        }

        var builder = new StringBuilder();

        for (int row = 0; row < 9; row++)
        {
            if (row % 3 == 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    var first = i % 3 == 0 ? "+" : "-";
                    builder.Append(first + StringUtility.Repeat('-', maxWidth));
                }

                builder.Append("+\n");
            }
            
            for (int col = 0; col < 9; col++)
            {
                var first = col % 3 == 0 ? "|" : " ";
                
                var toPut = translatable[row, col] == 0
                    ? translatable.PossibilitiesAt(row, col).ToSlimString()
                    : translatable[row, col].ToString();
                builder.Append(first + StringUtility.FillWith(toPut, ' ', maxWidth));
            }

            builder.Append("|\n");
        }
        
        for (int i = 0; i < 9; i++)
        {
            var first = i % 3 == 0 ? "+" : "-";
            builder.Append(first + StringUtility.Repeat('-', maxWidth));
        }

        builder.Append("+\n");

        return builder.ToString();
    }
}

public interface ITranslatable
{
    int this[int row, int col] { get; }
    Possibilities PossibilitiesAt(int row, int col);
}