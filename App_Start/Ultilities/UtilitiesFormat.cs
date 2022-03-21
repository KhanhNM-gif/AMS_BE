using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public class UtilitiesFormat
{
    public static string FormatProfileCategory(string profileCategoryName)
    {
        var arr = profileCategoryName.Split('(');
        if (arr.Count() > 1)
        {
            string name1 = arr[0];
            string name2 = arr[1];
            string name3 = name2.Substring(0, 1).ToUpper();
            string name4 = name2.Substring(1);
            name1 = name1 + "<br/>" + "(" + name3 + name4;
            return name1;
        }
        else
        {
            return profileCategoryName;
        }
    }

    public static string SubContent(string str, int leng)
    {
        if (str.Length <= leng)
        {
            return str;
        }
        else
        {
            return str.Substring(0, leng - 1) + "...";
        }
    }

    public static string SubToolTipContent(string str, int leng)
    {
        if (str.Length <= leng)
        {
            return "";
        }
        else
        {
            return str;
        }
    }

    public static string FormatDateToUpdateDB(DateTime dt)
    {
        return string.Format("{0:yyyy-MM-dd HH:mm:ss}", dt);
    }

    public static string FormatDateToStringShort(DateTime dt)
    {
        if (dt.Year == DateTime.Now.Year)
            return string.Format("{0:dd/MM}", dt);
        else
            return string.Format("{0:dd/MM/yyyy}", dt);
    }

    public static string FormatDateToString(DateTime dt)
    {
        return string.Format("{0:dd/MM/yyyy}", dt);
    }

    public static string FormatDateToString(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return string.Format("{0:dd/MM/yyyy}", DateTime.Parse(str));
    }
    public enum DateTimeType
    {
        DDMMYYYY = 0,
        YYYY = 1,
        MMYYYY = 2
    }
    public static string GetDateTimeAndType(string strDT, out int dateTimeType)
    {
        dateTimeType = (int)DateTimeType.DDMMYYYY;
        switch (strDT.Length)
        {
            case 4:
                dateTimeType = (int)DateTimeType.YYYY;
                return "01/01/" + strDT;
            case 7:
                dateTimeType = (int)DateTimeType.MMYYYY;
                return "01/" + strDT;
            case 10:
                dateTimeType = (int)DateTimeType.DDMMYYYY;
                return strDT;
            default:
                return "";
        }
    }
    public static string GetDateTimeStringByType(DateTime dt, int dateTimeType)
    {
        switch (dateTimeType)
        {
            case (int)DateTimeType.DDMMYYYY:
                return string.Format("{0:dd/MM/yyyy}", dt);
            case (int)DateTimeType.YYYY:
                return string.Format("{0:yyyy}", dt);
            case (int)DateTimeType.MMYYYY:
                return string.Format("{0:MM/yyyy}", dt);
            default:
                return "";
        }
    }

    public static string FormatHSName(string itemName)
    {
        var arr = itemName.Split('(');
        if (arr.Count() > 1)
        {
            string name1 = arr[0];
            string name2 = arr[1];
            name2 = name2.Replace(")", "");
            return name2 + " - " + name1;
        }
        else
        {
            return itemName;
        }
    }

    public static string FormatDateTimeToString(DateTime dt)
    {
        //if (dt.Year == DateTime.Now.Year)
        //    return string.Format("{0:dd/MM HH:mm}", dt);
        //else
        return string.Format("{0:dd/MM/yyyy HH:mm}", dt);
    }

    public static string FormatDateTimeToStringShort(DateTime dt)
    {
        if (dt.Hour == 0 && dt.Minute == 0)
            return string.Format("{0:dd/MM/yyyy}", dt);
        else
            return string.Format("{0:dd/MM/yyyy HH:mm}", dt);
    }

    public static string FormatDateTimeToStringFullMinute(DateTime dt)
    {
        return string.Format("{0:dd/MM/yyyy HH:mm}", dt);
    }

    public static string FormatDateTimeToString(DateTime dt, int type)
    {
        if (type == 1)
        {
            return string.Format("{0:dd/MM/yyyy HH:mm tt}", dt);
        }
        else if (type == 2)
        {
            return string.Format("{0:dd/MM/yyyy - HH:mm}", dt);
        }
        else if (type == 3)
        {
            return string.Format("{0:dd/MM/yyyy - HH:mm}", dt);
        }
        else
        {
            return string.Format("{0:dd/MM/yyyy}", dt);
        }
    }

    public static string FormatMoneyEmptyWhenZero(object money)
    {
        return FormatMoney(money, true);
    }
    public static string FormatMoney(object money)
    {
        return FormatMoney(money, false);
    }
    public static string FormatMoney(object money, bool isEmptyWhenZero)
    {
        try
        {
            if (money == null || money is DBNull)
                return "";

            if (money != null && string.IsNullOrEmpty(money.ToString()))
                return "";

            float fmoney = float.Parse(money.ToString());
            if (fmoney == 0)
            {
                if (isEmptyWhenZero)
                    return "";
                else
                    return "0";
            }
            else
                if (fmoney >= 1000)
            {
                CultureInfo cul = CultureInfo.GetCultureInfo("en-US");   // try with "en-US"
                return Convert.ToDouble(money).ToString("#,###", cul.NumberFormat);//.Replace('.', ',');
            }
            else
                return money.ToString();
        }
        catch (Exception ex)
        {
            BSS.Log.WriteErrorLog(ex.ToString());
            return "";
        }
    }


    public static string FormatNumber(object number)
    {
        try
        {
            if (number is DBNull)
                return "";

            if (number != null && string.IsNullOrEmpty(number.ToString()))
                return "";

            if (number.ToString() == "0")
                return "0";
            else
                return Convert.ToDecimal(number).ToString("0,0", CultureInfo.CreateSpecificCulture("el-GR"));
        }
        catch
        {
            throw;
        }
    }

    public static string FormatPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return "";
        else
            if (phoneNumber.IndexOf(",") != -1 || phoneNumber.IndexOf(",") != -1)
            return phoneNumber;
        else
            return phoneNumber.Substring(1, 3) + '.' +
                   phoneNumber.Substring(4, 3) + '.' +
                   phoneNumber.Substring(7);
    }
}