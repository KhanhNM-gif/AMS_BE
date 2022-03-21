using ASM_API.App_Start.TableModel;
using System;
using System.Collections.Generic;
using System.Linq;

public interface ILogUpdate<T>
{
    string SetInfoChangeRequest(T o);
    string GetInfoChangeRequest();
}

public interface IMappingModel
{
    string GetDifferences(object obj_new, object obj_old, out string strChange);
}

public interface IMappingSingleField : IMappingModel
{
    string GetName();
    string GetOne(object k, out IMappingSingleField outModel);
}

public static class MappingSingleField
{
    public static string GetDifferences(IMappingSingleField mapping, object obj_new, object obj_old, out string strChange)
    {
        strChange = null;
        IMappingSingleField iMappingModel;

        string valueMappingNew, valueMappingOld;

        string msg = mapping.GetOne(obj_new, out iMappingModel);
        if (msg.Length > 0) return msg;
        else valueMappingNew = iMappingModel is null ? "Chưa chọn" : iMappingModel.GetName();

        msg = mapping.GetOne(obj_old, out iMappingModel);
        if (msg.Length > 0) return msg;
        else valueMappingOld = iMappingModel is null ? "Chưa chọn" : iMappingModel.GetName();

        strChange = $"{ valueMappingOld} ==> { valueMappingNew}";

        return string.Empty;
    }
}

public class MappingDateTime : IMappingSingleField
{
    private const string dateFormat = "dd/MM/yyyy";

    private MappingDateTime(DateTime date)
    {
        this.date = date;
    }
    public MappingDateTime() { }

    private DateTime date;
    public string GetName() => date.ToString(dateFormat);

    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = null;

        if (k is DateTime) { outModel = new MappingDateTime((DateTime)k); }
        else return "Key is not a datetime data type";

        return string.Empty;
    }

    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }
}

public class MappingObject : IMappingSingleField
{

    private MappingObject(object obj)
    {
        this.obj = obj;
    }
    public MappingObject() { }

    private object obj;
    public string GetName() => obj is null ? "" : obj.ToString();

    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = new MappingObject(k);

        return string.Empty;
    }

    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }
}

interface IMappingList : IMappingModel
{
}
public class MappingList<T> : IMappingList, IMappingModel where T : class, IKeyCompare
{
    string separator;
    public MappingList()
    {
        this.separator = ",";
    }
    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        strChange = "";

        List<string> LtChanges = new List<string>();

        if (obj_new is List<T> list_new && obj_old is List<T> list_old)
        {
            var logRemove = list_old
                 .Except(list_new, new IModelCompare<T>())
                 .Select(x => $"Xóa {x.DisplayNameKey()} {x.GetKey()}");

            LtChanges.AddRange(logRemove);

            var dicItemProposalFormDetails = list_old.ToDictionary(x => x.GetKey(), y => y);
            foreach (var item in list_new)
            {
                if (!dicItemProposalFormDetails.TryGetValue(item.GetKey(), out var outItem)) { LtChanges.Add($"Thêm {item.DisplayNameKey()} {item.GetKey()}"); }
                else
                {
                    string msg = item.GetUpdateInfo3(outItem, separator, out string s);
                    if (msg.Length > 0) return msg;

                    if (!string.IsNullOrEmpty(s)) LtChanges.Add($"Sửa {item.DisplayNameKey()} {item.GetKey()}: {s}");
                }
            }

            strChange = string.Join(separator, LtChanges);
        }

        return string.Empty;


    }
}


public class MappingAttribute : Attribute
{
    public MappingAttribute(string DisplayName, Type objectType)
    {
        MappingModel = (IMappingModel)Activator.CreateInstance(objectType);
        this.DisplayName = DisplayName;
    }
    public IMappingModel MappingModel { get; set; }
    public string DisplayName { get; set; }
}