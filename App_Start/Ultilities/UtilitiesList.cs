using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;

/// <summary>
/// Summary description for UtilitiesFile
/// </summary>
public static class UtilitiesList
{
    public static void MoveIndex<T>(this List<T> list, int srcIdx, int destIdx)
    {
        if (srcIdx < 0) return;
        if (destIdx < 0) return;

        if (srcIdx != destIdx)
        {
            list.Insert(destIdx, list[srcIdx]);
            list.RemoveAt(destIdx < srcIdx ? srcIdx + 1 : srcIdx);
        }
    }

    public static void SwapItems<T>(this List<T> list, int idxX, int idxY)
    {
        if (idxX < 0) return;
        if (idxY < 0) return;

        if (idxX != idxY)
        {
            T tmp = list[idxX];
            list[idxX] = list[idxY];
            list[idxY] = tmp;
        }
    }
}