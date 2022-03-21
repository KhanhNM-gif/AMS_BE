using BSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class FileAttachObject
{
    public Guid FileAttachGUID { get; set; }
    public Guid ObjectGUID { get; set; }
    public bool IsDelete { get; set; }

    public static string InsertUpdate(DBM dbm, Guid FileAttachGUID, Guid ObjectGUID, bool IsDelete)
    {
        FileAttachObject fao = new FileAttachObject { FileAttachGUID = FileAttachGUID, ObjectGUID = ObjectGUID, IsDelete = IsDelete };
        return fao.InsertUpdate(dbm);
    }
    public string InsertUpdate(DBM dbm)
    {
        string msg = dbm.SetStoreNameAndParams("usp_FileAttachObject_InsertUpdate",
           new
           {
               FileAttachGUID,
               ObjectGUID,
               IsDelete
           });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
}