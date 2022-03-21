using System;
using System.Collections.Generic;
using BSS;
using System.Data;

public class AssetUse
{
    public const int CategoryHistory_HandOver = 1, CategoryHistory_Return = 2;

    public DateTime ExecutionDate { get; set; }
    public string UserPerformer { get; set; }
    public string UserConfirm { get; set; }
    public string Content { get; set; }
    public int CategoryHistory { get; set; }
    public static string GetListHistoryUse(long AssetId, out List<AssetUse> lt)
    {
        return DBM.GetList("usp_Asset_GetHistoryUse", new { AssetId }, out lt);
    }

    public static string GetSumTimeUse(List<AssetUse> LtAssetUse, out double TotalTimeUse)
    {
        TotalTimeUse = 0;

        int i = 0;
        if (LtAssetUse.Count == 0) return "";
        while (true)
        {
            AssetUse StartObject = LtAssetUse[i];
            if (StartObject.CategoryHistory == AssetUse.CategoryHistory_HandOver)
            {
                DateTime StartDate = LtAssetUse[i].ExecutionDate;

                i = i + 1;

                DateTime EndDate = DateTime.Now;
                while (true)
                {
                    if (i >= LtAssetUse.Count) break;

                    if (LtAssetUse[i].CategoryHistory == AssetUse.CategoryHistory_Return)
                    {
                        EndDate = LtAssetUse[i].ExecutionDate;
                        i = i + 1;

                        break;
                    }
                    else i = i + 1;
                }

                TotalTimeUse += (EndDate - StartDate).TotalDays;
            }
            else i = i + 1;

            if (i >= LtAssetUse.Count) break;
        }

        return "";
    }
}