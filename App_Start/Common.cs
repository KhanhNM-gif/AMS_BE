using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;


public static class Common
{
    public static string GetClientIpAddress(this HttpRequestMessage request)
    {
        if (request.Properties.ContainsKey("MS_HttpContext"))
        {
            dynamic ctx = request.Properties["MS_HttpContext"];
            if (ctx != null)
            {
                return ctx.Request.UserHostAddress;
            }
        }

        if (request.Properties.ContainsKey("System.ServiceModel.Channels.RemoteEndpointMessageProperty"))
        {
            dynamic remoteEndpoint = request.Properties["System.ServiceModel.Channels.RemoteEndpointMessageProperty"];
            if (remoteEndpoint != null)
            {
                return remoteEndpoint.Address;
            }
        }

        return null;
    }

    public static string GetUpdateInfo<T>(this T newValue, T oldValue, string separation, params Tuple<string, string, Dictionary<object, string>>[] mapProperties) where T : class
    {
        Dictionary<string, Tuple<object, object>> dic = new Dictionary<string, Tuple<object, object>>();
        foreach (var f in typeof(T).GetProperties())
            dic.Add(f.Name, new Tuple<object, object>(f.GetValue(newValue), f.GetValue(oldValue)));

        List<string> LtChanges = new List<string>();

        string valueMappingNew, valueMappingOld, nameField;

        foreach (var property in mapProperties)
        {

            if (dic.TryGetValue(property.Item1, out var tuple1))
            {
                if (property.Item3 is null || !property.Item3.TryGetValue(tuple1.Item1, out valueMappingNew))
                {
                    if (tuple1.Item1 is DateTime dateTime) valueMappingNew = dateTime.ToString("dd/MM/yyyy");
                    else if (property.Item3 is null) valueMappingNew = tuple1.Item1 is null ? "" : tuple1.Item1.ToString();
                    else valueMappingNew = "Chưa chọn";
                }

                if (property.Item3 is null || !property.Item3.TryGetValue(tuple1.Item2, out valueMappingOld))
                {
                    if (tuple1.Item2 is DateTime dateTime) valueMappingOld = dateTime.ToString("dd/MM/yyyy");
                    else if (property.Item3 is null) valueMappingOld = tuple1.Item2 is null ? "" : tuple1.Item2.ToString();
                    else valueMappingOld = "Chưa chọn";
                }
            }
            else return $"{property.Item1} field not found";

            nameField = string.IsNullOrEmpty(property.Item2) ? property.Item1 : property.Item2;

            if (valueMappingNew.Equals(valueMappingOld)) continue;

            LtChanges.Add($"Sửa {nameField}: {valueMappingOld} ==> {valueMappingNew}");
        };

        return string.Join(separation, LtChanges);
    }

    public static string GetUpdateInfo2<T>(this ILogUpdate<T> newValue, ILogUpdate<T> oldValue, string separation, out string infoUpdate, params Tuple<string, string, IMappingModel>[] mapProperties) where T : class
    {
        infoUpdate = "";

        string msg;
        Dictionary<string, Tuple<object, object>> dic = new Dictionary<string, Tuple<object, object>>();
        foreach (var f in typeof(T).GetProperties())
            dic.Add(f.Name, new Tuple<object, object>(f.GetValue(newValue), f.GetValue(oldValue)));

        List<string> LtChanges = new List<string>();

        string strChange, nameField;

        foreach (var property in mapProperties)
        {
            if (dic.TryGetValue(property.Item1, out var tuple1))
            {
                if (object.Equals(tuple1.Item1, tuple1.Item2)) continue;

                if (property.Item3 != null)
                {
                    msg = property.Item3.GetDifferences(tuple1.Item1, tuple1.Item2, out strChange);
                    if (msg.Length > 0) return msg;
                }
                else return "IMappingSingleField null";

            }
            else return $"{property.Item1} field not found";

            nameField = string.IsNullOrEmpty(property.Item2) ? property.Item1 : property.Item2;

            LtChanges.Add($"Sửa {nameField}: {strChange}");
        };

        infoUpdate = string.Join(separation, LtChanges);

        return string.Empty;
    }

    public static string GetUpdateInfo3<T>(this T newValue, T oldValue, string separation, out string infoUpdate) where T : class
    {
        infoUpdate = "";

        string msg;
        Dictionary<string, Tuple<object, object, IMappingModel, string>> dic = new Dictionary<string, Tuple<object, object, IMappingModel, string>>();
        foreach (var f in typeof(T).GetProperties())
        {
            var customAttributes = (MappingAttribute[])f.GetCustomAttributes(typeof(MappingAttribute), true);
            if (customAttributes.Length > 0)
                dic.Add(f.Name, new Tuple<object, object, IMappingModel, string>(f.GetValue(newValue), f.GetValue(oldValue), customAttributes[0].MappingModel, customAttributes[0].DisplayName));
        }

        List<string> LtChanges = new List<string>();

        string strChange, nameField;

        foreach (var property in dic)
        {
            if (object.Equals(property.Value.Item1, property.Value.Item2)) continue;

            if (property.Value.Item3 != null)
            {
                msg = property.Value.Item3.GetDifferences(property.Value.Item1, property.Value.Item2, out strChange);
                if (msg.Length > 0) return msg;
            }
            else return "IMappingSingleField null";

            nameField = string.IsNullOrEmpty(property.Value.Item4) ? property.Key : property.Value.Item4;

            LtChanges.Add($"Sửa {nameField}: {strChange}");
        };

        infoUpdate = string.Join(separation, LtChanges);

        return string.Empty;
    }

    public static string GetIPAddress(this HttpRequest request)
    {
        string ipAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

        if (string.IsNullOrEmpty(ipAddress)) ipAddress = request.ServerVariables["REMOTE_ADDR"];

        return ipAddress;
    }
    public static string GetIPAddress()
    {
        HttpContext context = HttpContext.Current;
        if (context == null) return "";

        HttpRequest request = context.Request;
        if (request == null) return "";

        string ipAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

        if (string.IsNullOrEmpty(ipAddress)) ipAddress = request.ServerVariables["REMOTE_ADDR"];

        return ipAddress;
    }

    public static string ToObject<T>(this JObject data, string key, out T t)
    {
        t = default(T);
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return "key is null or empty @JObjectToOject";
            if (data[key] == null) return string.Format("data[key] is null (key: {0})", key);

            t = data[key].ToObject<T>();
            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    public static string ToString(this JObject data, string key, out string str)
    {
        str = "";
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return "key is null or empty @JObjectToString";
            if (data[key] == null) return string.Format("data[key] is null (key: {0})", key);

            str = data[key].ToString();
            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    public static string ToNumber(this JObject data, string key, out int number)
    {
        number = 0;
        try
        {
            string msg = data.ToString(key, out string str);
            if (msg.Length > 0) return msg;

            number = int.Parse(str);
            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    public static string ToGuid(this JObject data, string key, out Guid guid)
    {
        guid = Guid.Empty;
        try
        {
            string msg = data.ToString(key, out string str);
            if (msg.Length > 0) return msg;

            guid = Guid.Parse(str);
            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    public static string ToDateTime(this JObject data, string key, out DateTime date)
    {
        date = DateTime.Now;
        try
        {
            string msg = data.ToString(key, out string str);
            if (msg.Length > 0) return msg;

            date = DateTime.Parse(str);
            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    public static bool IsValidDateTime(string value)
    {
        DateTime dateTime;
        if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out dateTime))
        {
            return true;
        }
        return false;
    }
    public static string ToBool(this JObject data, string key, out bool b)
    {
        b = true;
        try
        {
            string msg = data.ToString(key, out string str);
            if (msg.Length > 0) return msg;

            b = bool.Parse(str);
            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    public static byte[] GenerateRandomBytes(int length)
    {
        var result = new byte[length];
        RandomNumberGenerator.Create().GetBytes(result);
        return result;
    }
    public static byte[] GetInputPasswordHash(string pwd, byte[] salt)
    {
        int passwordDerivationIteration = 1000;
        int passwordBytesLength = 64;
        var inputPwdBytes = Encoding.UTF8.GetBytes(pwd);
        var inputPwdHasher = new Rfc2898DeriveBytes(inputPwdBytes, salt, passwordDerivationIteration);
        return inputPwdHasher.GetBytes(passwordBytesLength);
    }
}