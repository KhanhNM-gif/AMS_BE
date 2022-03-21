using BSS;
using BSS.DataValidator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class Tag
{
    public int TagID { get; set; }
    [JsonIgnore]
    public Guid ObjectGuid { get; set; }
    public string TagName { get; set; }
    [JsonIgnore]
    public int UserIDCreate { get; set; }
    public bool IsDelete { get; set; }
    [JsonIgnore]
    public DateTime LastUpdate { get; set; }
    [JsonIgnore]
    public DateTime CreateDate { get; set; }

    public string InsertUpdate(DBM dbm, out Tag d)
    {
        d = null;

        string msg = dbm.SetStoreNameAndParams("usp_Tag_InsertUpdate",
            new
            {
                TagID,
                TagName = TagName.Trim(),
                UserIDCreate
            });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out d);
    }

    public static string GetOne(int TagID, out Tag o)
    {
        return DBM.GetOne("usp_Tag_SelectByTagID", new { TagID }, out o);
    }

    public static string GetList(string TagName, int StatusID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Tag_SelectSearch", new {TagName, StatusID }, out dt);
    }

    public static string GetSuggestSearch(string TagName, int StatusID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Tag_SelectSuggestSearch", new { TagName, StatusID }, out dt);
    }

    public static string GetByTagName(string TagName, out List<Tag> ltTag)
    {
        return DBM.GetList("usp_Tag_SelectByTagName", new { TagName }, out ltTag);
    }

    public static string UpdateDelete(int TagID, bool IsDelete)
    {
        return DBM.ExecStore("usp_Tag_UpdateIsDelete", new { TagID, IsDelete });
    }

    public static string ValidateTag(Tag tag)
    {
        if (string.IsNullOrEmpty(tag.TagName)) return "Tên nhãn không được để trống".ToMessageForUser();

        string  msg = DataValidator.Validate(tag).ToErrorMessage();
        if (msg.Length > 0) return msg.ToMessageForUser();

        List<Tag> ltTag;
        msg = Tag.GetByTagName(tag.TagName.Trim(), out ltTag);
        if (msg.Length > 0) return msg;
        if (ltTag.Where(v => !v.IsDelete && v.TagID != tag.TagID).Count() > 0) return ("Đã tồn tại Tên nhãn '" + tag.TagName + "'").ToMessageForUser();

        return "";
    }
}