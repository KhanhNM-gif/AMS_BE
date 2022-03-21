using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class UtilitiesString
{
    public static string EncodeStringToBase64(string stringToEncode)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(stringToEncode));
    }

    public static string DecodeStringFromBase64(string stringToDecode)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(stringToDecode));
    }

    public static byte[] FromBase64Url(string base64Url)
    {
        string padded = base64Url.Length % 4 == 0
            ? base64Url : base64Url + "====".Substring(base64Url.Length % 4);
        string base64 = padded.Replace("_", "/")
                              .Replace("-", "+");
        return Convert.FromBase64String(base64);
    }

    public static string GetText(object o, int length)
    {
        return GetText(o, length, "right");
    }

    public static string GetText(object o, int length, string tooltipPlace)
    {
        if (o == null)
            return "";

        string str = o.ToString();
        if (str.Length <= length + 3)
            return str;
        else
        {
            string[] arr = str.Split(' ');
            string strResult = "";
            foreach (string item in arr)
            {
                string strResultItem = strResult + " " + item;
                if (strResultItem.Length <= length)
                    strResult = strResultItem;
                else
                    break;
            }
            return "<span data-toggle='popover' data-trigger='hover' data-placement='" + tooltipPlace + "' data-content='" + str + "'>" + strResult + "...</span>";
        }
    }

    /// <summary>
    /// Ases the specified input.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input">The input.</param>
    /// <param name="parser">The parser.</param>
    /// <returns></returns>
    public static T As<T>(string input, Func<string, T> parser)
    {
        return String.IsNullOrEmpty(input) ? parser(input) : default(T);
    }

    /// <summary>
    /// Ors the specified input.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input">The input.</param>
    /// <param name="default">The @default.</param>
    /// <returns></returns>
    public static T Or<T>(object input, T @default) where T : class
    {
        return input == null ? @default : input as T;
    }

    /// <summary>
    /// String.Format with nicer syntax.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public static string FormatWith(this string source, params object[] parameters)
    {
        return String.Format(CultureInfo.InvariantCulture, source, parameters);
    }

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="encoding">The encoding.</param>
    /// <returns></returns>
    public static byte[] GetBytes(this string source, Encoding encoding = null)
    {
        return (encoding ?? Encoding.UTF8).GetBytes(source);
    }

    /// <summary>
    /// Surround the string with parentheses.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns></returns>
    public static string InParentheses(this string source)
    {
        return '"' + source + '"';
    }

    /// <summary>
    /// If the string is null or empty, return null.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns></returns>
    public static string AsNullIfEmpty(this string input)
    {
        return (String.IsNullOrEmpty(input) || String.IsNullOrWhiteSpace(input)) ? null : input;
    }

    public static string GetWordLowerLast(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        int index = input.Trim().LastIndexOf(" ");
        if (index == -1)
            return input.ToLower();
        else
            return (input.Substring(index + 1)).Trim().ToLower();
    }

    /// <summary>
    /// Gets the a dictionary of group name and group value from using regex.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns></returns>
    public static Dictionary<string, object> GetGroupValueDictionaryRegex(string content, string pattern)
    {
        var regex = new Regex(pattern);
        var match = regex.Match(content);

        var requestParams = new Dictionary<string, object>();
        for (var i = 0; i < match.Groups.Count; i++)
        {
            var name = regex.GroupNameFromNumber(i);
            var value = match.Groups[i].Value;
            requestParams.Add(name, value);
        }
        return requestParams;
    }

    public static string Get10NumberRandom()
    {
        var random = new Random();
        string s = string.Empty;
        for (int i = 0; i < 10; i++)
            s = String.Concat(s, random.Next(10).ToString());
        return s;
    }

    /// <summary>
    /// Extension method to determine whether the string contain any character that is outside the GSM-7bit alphabet.
    /// REF:http://www.smsmac.com/help/discover/about-sms/gsm7bit/
    /// </summary>
    /// <param name="str">The string to be tested.</param>
    /// <returns>
    /// 	<c>true</c> at the first char that fall outside GSM-7bit alphabet is found <c>false if none is found</c>.
    /// </returns>
    /// <remarks>
    /// Performance: depends on the string's length. Best case: O(1), worst case: O(n), average case: O(n)
    /// </remarks>
    public static bool IsGsmString(this string str)
    {
        //test if the char code is between 0 and 2^7-1
        if (String.IsNullOrEmpty(str))
        {
            return true;
        }
        foreach (var chr in str)
        {
            var code = (int)chr;
            if (code < 0 || code > 127)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Splits the input string using into constituents using delimiters: ' ', ',' ';'.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>An array of constituents</returns>
    public static string[] Split(string input)
    {
        return Split(input, new[] { ' ', ',', ';', '|' });
    }

    /// <summary>
    /// Splits the input string using into constituents using user-provided delimiters.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="delimiters">The delimiters.</param>
    /// <returns></returns>
    public static string[] Split(string input, char[] delimiters)
    {
        return String.IsNullOrEmpty(input)
                ? new string[0]
                : input.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Trims and remove to last space.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="length">The length.</param>
    /// <returns></returns>
    public static string TrimRemoveToLastSpace(this string input, int length)
    {
        if (input == null) { return ""; }

        var output = input.Trim();
        if (length < output.Length)
        {
            output = output.Substring(0, length);
        }

        return output;
    }

    private static readonly string[] VietnameseSigns = new string[]

    {

        "aAeEoOuUiIdDyY",

        "áàạảãâấầậẩẫăắằặẳẵ",

        "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",

        "éèẹẻẽêếềệểễ",

        "ÉÈẸẺẼÊẾỀỆỂỄ",

        "óòọỏõôốồộổỗơớờợởỡ",

        "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",

        "úùụủũưứừựửữ",

        "ÚÙỤỦŨƯỨỪỰỬỮ",

        "íìịỉĩ",

        "ÍÌỊỈĨ",

        "đ",

        "Đ",

        "ýỳỵỷỹ",

        "ÝỲỴỶỸ"

    };



    public static string RemoveSignVietnamese(string str)
    {
        //string message = "";
        //string strTemp = ConvertTcvn3ToUnicode(str, ref  message);
        //if (message.Length ==  0) str = strTemp;
        //Tiến hành thay thế , lọc bỏ dấu cho chuỗi

        for (int i = 1; i < VietnameseSigns.Length; i++)
        {

            for (int j = 0; j < VietnameseSigns[i].Length; j++)

                str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);

        }

        return str;

    }

    public static string ConvertAndRemoveSignVietnamese(string str)
    {
        string message = "";
        string strTemp = ConvertTcvn3ToUnicode(str, ref message);
        if (message.Length == 0) str = strTemp;

        return RemoveSignVietnamese(str);
    }

    ///// <summary>
    ///// Strips the vietnamese chars from the input string.
    ///// </summary>
    ///// <param name="input">The input.</param>
    ///// <returns></returns>
    //public static string StripVietnameseChars(this string input)
    //{
    //    string[] vietnameseChars =
    //        {
    //            "áàảãạăắằẳẵặâấầẩẫậ",
    //            "ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬ",
    //            "đ",
    //            "Đ",
    //            "éèẻẽẹêếềểễệ",
    //            "ÉÈẺẼẸÊẾỀỂỄỆ",
    //            "íìỉĩị",
    //            "ÍÌỈĨỊ",
    //            "óòỏõọơớờởỡợôốồổỗộ",
    //            "ÓÒỎÕỌƠỚỜỞỠỢÔỐỒỔỖỘ",
    //            "ưứừửữựúùủũụ",
    //            "ƯỨỪỬỮỰÚÙỦŨỤ",
    //            "ýỳỷỹỵ",
    //            "ÝỲỶỸỴ"
    //        };

    //    var latinChars = new[]
    //        {
    //            'a',
    //            'A',
    //            'd',
    //            'D',
    //            'e',
    //            'E',
    //            'i',
    //            'I',
    //            'o',
    //            'O',
    //            'u',
    //            'U',
    //            'y',
    //            'Y'
    //        };
    //    return input.ReplaceCharGroups(vietnameseChars, latinChars);
    //}

    /// <summary>
    /// Strips the delimiters.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns></returns>
    public static string StripDelimiters(this string input)
    {
        var delimiters = new[] { '|', '"', '\'', ';', ',', '.' };
        return input.StripChars(delimiters);
    }

    /// <summary>
    /// Strips the specified characters.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="strips">The strips.</param>
    /// <returns></returns>
    public static string StripChars(this string input, params char[] strips)
    {
        if (strips == null)
        {
            throw new ArgumentNullException("strips");
        }

        var scanner = new StringBuilder(input);
        var builder = new StringBuilder(scanner.Length);
        for (var i = 0; i < scanner.Length; i++)
        {
            if (strips.Any(c => scanner[i].Equals(c)))
            {
                continue;
            }
            builder.Append(scanner[i]);
        }

        return builder.ToString();
    }

    ///// <summary>
    ///// Replaces groups of characters with another character.
    ///// </summary>
    ///// <param name="input">The input.</param>
    ///// <param name="strips">The strips.</param>
    ///// <param name="replacements">The replacements.</param>
    ///// <returns></returns>
    //public static string ReplaceCharGroups(this string input, string[] strips, char[] replacements)
    //{
    //    if (strips == null)
    //    {
    //        throw new ArgumentNullException("strips");
    //    }
    //    if (replacements == null)
    //    {
    //        throw new ArgumentNullException("replacements");
    //    }
    //    if (strips.Length > replacements.Length)
    //    {
    //        throw new ArgumentException("Length of replacement array must be larger than strip array");
    //    }

    //    var scanner = new StringBuilder(input);
    //    for (var i = 0; i < scanner.Length; i++)
    //    {
    //        for (var j = 0; j < strips.Length; j++)
    //        {
    //            if (strips[j].IndexOf(scanner[i]) != -1)
    //            {
    //                scanner[i] = replacements[j];
    //            }
    //        }

    //    }
    //    return scanner.ToString();
    //}

    ///<summary>
    /// Convert TCVN3 to Unicode
    ///</summary>
    ///<param name="input">The input</param>
    ///<returns></returns>
    public static string ConvertTcvn3ToUnicode(string input, ref string message)
    {
        try
        {
            char[] tcvn3Char = {'\u00FC', '\u00FB', '\u00FE', '\u00FA', '\u00F9', '\u00F7', '\u00F6',
                '\u00F5', '\u00F8', '\u00F1', '\u00F4', '\u00EE', '\u00EC', '\u00EB', '\u00EA', '\u00ED',
                '\u00E9', '\u00E7', '\u00E6', '\u00E5', '\u00E8', '\u00E1', '\u00E4', '\u00DE', '\u00D8',
                '\u00D6', '\u00D4', '\u00D3', '\u00D2', '\u00D5', '\u00CF', '\u00CE', '\u00D1', '\u00C6',
                '\u00BD', '\u00BC', '\u00AB', '\u00BE', '\u00CB', '\u00C9', '\u00C8', '\u00C7', '\u00CA',
                '\u00B6', '\u00B9', '\u00AD', '\u00A6', '\u00AC', '\u00A5', '\u00F2', '\u00DC', '\u00AE',
                '\u00A8', '\u00A1', '\u00F3', '\u00EF', '\u00E2', '\u00BB', '\u00E3', '\u00DF', '\u00DD',
                '\u00D7', '\u00AA', '\u00D0', '\u00CC', '\u00B7', '\u00A9', '\u00B8', '\u00B5', '\u00A4',
                '\u00A7', '\u00A3', '\u00A2',
                '\u00B5', '\u00B6', '\u00C3', '\u00B8', '\u00B9',
                '\u00CC', '\u00CE', '\u00CF', '\u00D0', '\u00D1',
                '\u00D7', '\u00D8', '\u00DC', '\u00CD', '\u00DE',
                '\u00DF', '\u00E1', '\u00E2', '\u00E3', '\u00E4',
                '\u00D9', '\u00F1', '\u00F2', '\u00DA', '\u00F4',
                '\u00FA', '\u00FB', '\u00FC', '\u00FD', '\u00FE',
                '\u00BB', '\u00BC', '\u00BD', '\u00BE', '\u00C6',
                '\u00C7', '\u00C8', '\u00C9', '\u00CA', '\u00CB',
                '\u00D2', '\u00D3', '\u00D4', '\u00D5', '\u00D6',
                '\u00E5', '\u00E6', '\u00E7', '\u00E8', '\u00E9',
                '\u00EA', '\u00EB', '\u00EC', '\u00ED', '\u00EE',
                '\u00F5', '\u00F6', '\u00F7', '\u00F8', '\u00F9'};
            char[] unicodeChar = {'\u1EF9', '\u1EF7', '\u1EF5', '\u1EF3', '\u1EF1', '\u1EEF', '\u1EED',
                '\u1EEB', '\u1EE9', '\u1EE7', '\u1EE5', '\u1EE3', '\u1EE1', '\u1EDF', '\u1EDD', '\u1EDB',
                '\u1ED9', '\u1ED7', '\u1ED5', '\u1ED3', '\u1ED1', '\u1ECF', '\u1ECD', '\u1ECB', '\u1EC9',
                '\u1EC7', '\u1EC5', '\u1EC3', '\u1EC1', '\u1EBF', '\u1EBD', '\u1EBB', '\u1EB9', '\u1EB7',
                '\u1EB5', '\u1EB3', '\u00F4', '\u1EAF', '\u1EAD', '\u1EAB', '\u1EA9', '\u1EA7', '\u1EA5',
                '\u1EA3', '\u1EA1', '\u01B0', '\u01AF', '\u01A1', '\u01A0', '\u0169', '\u0129', '\u0111',
                '\u0103', '\u0102', '\u00FA', '\u00F9', '\u00F5', '\u1EB1', '\u00F3', '\u00F2', '\u00ED',
                '\u00EC', '\u00EA', '\u00E9', '\u00E8', '\u00E3', '\u00E2', '\u00E1', '\u00E0', '\u00D4',
                '\u0110', '\u00CA', '\u00C2',
                '\u00C0', '\u1EA2', '\u00C3', '\u00C1', '\u1EA0', '\u00C8',
                '\u1EBA', '\u1EBC', '\u00C9', '\u1EB8', '\u00CC', '\u1EC8', '\u0128', '\u00CD', '\u1ECA',
                '\u00D2', '\u1ECE', '\u00D5', '\u00D3', '\u1ECC', '\u00D9', '\u1EE6', '\u0168', '\u00DA',
                '\u1EE4', '\u1EF2', '\u1EF6', '\u1EF8', '\u00DD', '\u1EF4', '\u1EB0', '\u1EB2', '\u1EB4',
                '\u1EAE', '\u1EB6', '\u1EA6', '\u1EA8', '\u1EAA', '\u1EA4', '\u1EAC', '\u1EC0', '\u1EC2',
                '\u1EC4', '\u1EBE', '\u1EC6', '\u1ED2', '\u1ED4', '\u1ED6', '\u1ED0', '\u1ED8', '\u1EDC',
                '\u1EDE', '\u1EE0', '\u1EDA', '\u1EE2', '\u1EEA', '\u1EEC', '\u1EEE', '\u1EE8', '\u1EF0'};

            var str = input.Normalize(NormalizationForm.FormC);

            var result = "";
            foreach (var t in str)
            {
                var pos = -1;
                for (var j = 0; j < tcvn3Char.Length; j++)
                {
                    if (t != tcvn3Char[j]) continue;
                    pos = j;
                    break;
                }

                if (pos >= 0)
                {
                    result += unicodeChar[pos];
                }
                else
                {
                    result += t;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return input;
        }
    }

    ///<summary>
    /// Convert Unicode to TCVN3
    ///</summary>
    ///<param name="input">The input</param>
    ///<returns></returns>
    public static string ConvertUnicodeToTcvn3(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        char[] tcvn3Char = {'\u00FC', '\u00FB', '\u00FE', '\u00FA', '\u00F9', '\u00F7', '\u00F6',
                '\u00F5', '\u00F8', '\u00F1', '\u00F4', '\u00EE', '\u00EC', '\u00EB', '\u00EA', '\u00ED',
                '\u00E9', '\u00E7', '\u00E6', '\u00E5', '\u00E8', '\u00E1', '\u00E4', '\u00DE', '\u00D8',
                '\u00D6', '\u00D4', '\u00D3', '\u00D2', '\u00D5', '\u00CF', '\u00CE', '\u00D1', '\u00C6',
                '\u00BD', '\u00BC', '\u00AB', '\u00BE', '\u00CB', '\u00C9', '\u00C8', '\u00C7', '\u00CA',
                '\u00B6', '\u00B9', '\u00AD', '\u00A6', '\u00AC', '\u00A5', '\u00F2', '\u00DC', '\u00AE',
                '\u00A8', '\u00A1', '\u00F3', '\u00EF', '\u00E2', '\u00BB', '\u00E3', '\u00DF', '\u00DD',
                '\u00D7', '\u00AA', '\u00D0', '\u00CC', '\u00B7', '\u00A9', '\u00B8', '\u00B5', '\u00A4',
                '\u00A7', '\u00A3', '\u00A2',
                '\u00B5', '\u00B6', '\u00C3', '\u00B8', '\u00B9',
                '\u00CC', '\u00CE', '\u00CF', '\u00D0', '\u00D1',
                '\u00D7', '\u00D8', '\u00DC', '\u00CD', '\u00DE',
                '\u00DF', '\u00E1', '\u00E2', '\u00E3', '\u00E4',
                '\u00D9', '\u00F1', '\u00F2', '\u00DA', '\u00F4',
                '\u00FA', '\u00FB', '\u00FC', '\u00FD', '\u00FE',
                '\u00BB', '\u00BC', '\u00BD', '\u00BE', '\u00C6',
                '\u00C7', '\u00C8', '\u00C9', '\u00CA', '\u00CB',
                '\u00D2', '\u00D3', '\u00D4', '\u00D5', '\u00D6',
                '\u00E5', '\u00E6', '\u00E7', '\u00E8', '\u00E9',
                '\u00EA', '\u00EB', '\u00EC', '\u00ED', '\u00EE',
                '\u00F5', '\u00F6', '\u00F7', '\u00F8', '\u00F9'};
        char[] unicodeChar = {'\u1EF9', '\u1EF7', '\u1EF5', '\u1EF3', '\u1EF1', '\u1EEF', '\u1EED',
                '\u1EEB', '\u1EE9', '\u1EE7', '\u1EE5', '\u1EE3', '\u1EE1', '\u1EDF', '\u1EDD', '\u1EDB',
                '\u1ED9', '\u1ED7', '\u1ED5', '\u1ED3', '\u1ED1', '\u1ECF', '\u1ECD', '\u1ECB', '\u1EC9',
                '\u1EC7', '\u1EC5', '\u1EC3', '\u1EC1', '\u1EBF', '\u1EBD', '\u1EBB', '\u1EB9', '\u1EB7',
                '\u1EB5', '\u1EB3', '\u00F4', '\u1EAF', '\u1EAD', '\u1EAB', '\u1EA9', '\u1EA7', '\u1EA5',
                '\u1EA3', '\u1EA1', '\u01B0', '\u01AF', '\u01A1', '\u01A0', '\u0169', '\u0129', '\u0111',
                '\u0103', '\u0102', '\u00FA', '\u00F9', '\u00F5', '\u1EB1', '\u00F3', '\u00F2', '\u00ED',
                '\u00EC', '\u00EA', '\u00E9', '\u00E8', '\u00E3', '\u00E2', '\u00E1', '\u00E0', '\u00D4',
                '\u0110', '\u00CA', '\u00C2',
                '\u00C0', '\u1EA2', '\u00C3', '\u00C1', '\u1EA0', '\u00C8',
                '\u1EBA', '\u1EBC', '\u00C9', '\u1EB8', '\u00CC', '\u1EC8', '\u0128', '\u00CD', '\u1ECA',
                '\u00D2', '\u1ECE', '\u00D5', '\u00D3', '\u1ECC', '\u00D9', '\u1EE6', '\u0168', '\u00DA',
                '\u1EE4', '\u1EF2', '\u1EF6', '\u1EF8', '\u00DD', '\u1EF4', '\u1EB0', '\u1EB2', '\u1EB4',
                '\u1EAE', '\u1EB6', '\u1EA6', '\u1EA8', '\u1EAA', '\u1EA4', '\u1EAC', '\u1EC0', '\u1EC2',
                '\u1EC4', '\u1EBE', '\u1EC6', '\u1ED2', '\u1ED4', '\u1ED6', '\u1ED0', '\u1ED8', '\u1EDC',
                '\u1EDE', '\u1EE0', '\u1EDA', '\u1EE2', '\u1EEA', '\u1EEC', '\u1EEE', '\u1EE8', '\u1EF0'};

        var str = input.Normalize(NormalizationForm.FormC);

        var result = "";
        foreach (var t in str)
        {
            int pos = -1, j;
            var ch = t;
            for (j = unicodeChar.Length - 1; j >= 0; j--)
            {
                if (ch != unicodeChar[j]) continue;
                pos = j;
                break;
            }
            if (pos >= 0)
            {
                result += tcvn3Char[pos];
            }
            else
            {
                result += ch;
            }
        }

        return result;
    }

    public static string ConvertToUnicode(string input)
    {
        string message = "";
        string tcvn3 = ConvertUnicodeToTcvn3(input);
        string unicode = ConvertTcvn3ToUnicode(tcvn3, ref message);
        if (!string.IsNullOrEmpty(message))
            return input;

        return unicode;
    }

    ///<summary>
    /// Fixbug convert text in barcode to unicode ('-' => 'ư')
    ///</summary>
    ///<param name="source">The source</param>
    ///<returns></returns>
    public static string FixSpell(this string source)
    {
        string[] input = {"b-", "B-", "c-", "C-", "d-", "D-", "đ-", "Đ-",
                            "g-", "G-", "h-", "H-", "l-", "L-", "m-", "M-",
                            "n-", "N-", "p-", "P-", "r-", "R-", "s-", "S-",
                            "t-", "T-", "-u", "v-", "V-", "x-", "X-"};
        string[] output = {"bư", "Bư", "cư", "Cư", "dư", "Dư", "đư", "Đư",
                            "gư", "Gư", "hư", "Hư", "lư", "Lư", "mư", "Mư",
                            "nư", "Nư", "pư", "Pư", "rư", "Rư", "sư", "Sư",
                            "tư", "Tư", "ưu", "vư", "Vư", "xư", "Xư"};

        string[] input2 = {"-a", "-c", "-i", "-m", "-n", "-ng", "-p", "-t",
                            "-u", "gi-", "-ơc", "-ớc", "-ợc", "-ơi",
                            "-ởi", "-ới", "-ời", "-ợi", "-ỡi", "-ơm",
                            "-ợm", "-ởm", "-ớm", "-ờm", "-ỡm", "-ơn",
                            "-ợn", "-ởn", "-ớn", "-ờn", "-ỡn", "-ơp",
                            "-ớp", "-ợp", "-ơt", "-ợt", "-ớt", "-ơu",
                            "-ợu", "-ỡu", "-ởu", "-ớu", "-ờu", "ch-",
                            "Ch-", "cH-", "CH-", "nh-", "Nh-", "nH-",
                            "NH-", "ng-", "Ng-", "nG-", "NG-", "ph-",
                            "Ph-", "pH-", "PH-", "tr-", "Tr-", "tR-",
                            "TR-", "th-", "Th-", "tH-", "TH-"};
        string[] output2 = {"ưa", "ưc", "ưi", "ưm", "ưn", "ưng", "ưp", "ưt",
                            "ưu", "giư", "ươc", "ước", "ược", "ươi",
                            "ưởi", "ưới", "ười", "ượi", "ưỡi", "ươm",
                            "ượm", "ưởm", "ướm", "ườm", "ưỡm", "ươn",
                            "ượn", "ưởn", "ướn", "ườn", "ưỡn", "ươp",
                            "ướp", "ượp", "ươt", "ượt", "ướt", "ươu",
                            "ượu", "ưỡu", "ưởu", "ướu", "ườu", "chư",
                            "Chư", "cHư", "CHư", "như", "Như", "nHư",
                            "NHư", "ngư", "Ngư", "nGư", "NGư", "phư",
                            "Phư", "pHư", "PHư", "trư", "Trư", "tRư",
                            "TRư", "thư", "Thư", "tHư", "THư"};

        var arrayInput = source.Split(' ');
        var arrayOutput = new List<string>();
        foreach (var str in arrayInput)
        {
            var pos = -1;
            if (str.Length == 2)
            {
                for (var i = 0; i < input.Length; i++)
                {
                    if (str != input[i]) continue;
                    pos = i;
                    break;
                }
                arrayOutput.Add(pos > -1 ? output[pos] : str);
            }
            else if (str.Length > 2)
            {
                for (var i = 0; i < input2.Length; i++)
                {
                    if (!str.Contains(input2[i])) continue;
                    pos = i;
                    break;
                }
                arrayOutput.Add(pos > -1 ? str.Replace(input2[pos], output2[pos]) : str);
            }
            else
            {
                arrayOutput.Add(str);
            }
        }

        return String.Join(" ", arrayOutput.ToArray());
    }
}