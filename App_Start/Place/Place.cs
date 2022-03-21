using BSS;
using System;
using System.Collections.Generic;
using System.Linq;

public class Place : IMappingSingleField
{
    public int PlaceID { get; set; }
    public int PlaceType { get; set; }
    public Guid ObjectGuid { get; set; }
    public string PlaceCode { get; set; }
    public string PlaceFullName { get; set; }
    public string PlaceName { get; set; }
    public string PlaceDescription { get; set; }
    public int PlaceIDParent { get; set; }
    public string PlaceCodeParent { get; set; }
    public string PlaceNameParent { get; set; }
    public bool IsActive { get; set; }
    public int AccountID { get; set; }
    public long DiagramID { get; set; }
    public string DiagramLocation { get; set; }
    public string DiagramUrl { get; set; }

    public static string GetListByPlaceName(string PlaceName, int PlaceType, int AccountID, out List<Depot> lt)
    {
        return DBM.GetList("usp_Place_SearchByPlaceName", new { PlaceName, PlaceType, AccountID }, out lt);
    }
    public static string GetListByPlaceType(int PlaceType, int AccountID, out List<Place> lt)
    {
        return DBM.GetList("usp_Place_GetListByPlaceType", new { PlaceType, AccountID }, out lt);
    }
    public static string GetList(int AccountID, int UserID, int PlaceType, out List<Place> lt)
    {
        return DBM.GetList("usp_Place_GetList", new { AccountID, PlaceType, UserID }, out lt);
    }
    public static string GetListActive(int AccountID, int PlaceType, out List<Place> lt)
    {
        return DBM.GetList("usp_Place_GetListActive", new { AccountID, PlaceType }, out lt);
    }
    public static string GetListChild(int PlaceID, out string IDs)
    {
        return DBM.ExecStore("usp_Place_GetListChild", new { PlaceID }, out IDs);
    }
    public static string Delete(int PlaceID, int AccountID)
    {
        return DBM.ExecStore("usp_Place_DeleteByPlaceID", new { PlaceID, AccountID });
    }
    public static string GetOneByPlaceID(int PlaceID, int AccountID, out Place o)
    {
        return DBM.GetOne("usp_Place_SelectByPlaceID", new { PlaceID, AccountID }, out o);
    }
    public static string GetOneByPlaceIDParent(int PlaceID, int AccountID, out Place o)
    {
        return DBM.GetOne("usp_Place_SelectByPlaceIDParent", new { PlaceID, AccountID }, out o);
    }
    public static string GetOneObjectGuid(Guid ObjectGuid, out long placeID)
    {
        placeID = 0;

        string msg = DBM.GetOne("usp_Place_GetByObjectGuid", new { ObjectGuid }, out Place place);
        if (msg.Length > 0) return msg;

        if (place == null) return ("Không tồn tại Kho có ObjectGuid = " + ObjectGuid).ToMessageForUser();
        placeID = place.PlaceID;

        return msg;
    }
    public virtual string InsertUpdate(DBM dbm, out Place o)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Place_InsertUpdate",
                    new
                    {
                        PlaceID,
                        PlaceType,
                        PlaceIDParent,
                        PlaceCode,
                        PlaceName = PlaceName.Trim(),
                        PlaceDescription,
                        IsActive,
                        AccountID,
                        DiagramID,
                        DiagramLocation
                    }
                    );
        return dbm.GetOne(out o);
    }

    public string GetName() => PlaceName;

    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = null;

        string msg = DBM.GetOne("usp_Place_GetOne", new { PlaceID = (int)k }, out Place accountUser);
        if (msg.Length > 0) return msg;
        outModel = accountUser;

        return msg;
    }

    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }

    public virtual string CheckRole(int UserID)
    {
        throw new NotImplementedException();
    }
    public virtual string GetDisplayName()
    {
        throw new NotImplementedException();
    }
    public virtual string GetLogMessageInsertUpdate()
    {
        throw new NotImplementedException();
    }
    public virtual string SetData(int AccountID)
    {
        this.AccountID = AccountID;

        return string.Empty;
    }
    public virtual string Validate()
    {
        throw new NotImplementedException();
    }
}

public class StoragePlace : Place
{
    public override string CheckRole(int UserID)
    {
        return Role.Check(UserID, Constants.TabID.ND, Role.ROLE_ND_CRUD);
    }

    public override string GetDisplayName() => "Nơi để";
    public override string Validate() => "";
    public override string GetLogMessageInsertUpdate() => PlaceID == 0 ? "Thêm Nơi để tài sản" : "Sửa Nơi để tài sản";

}

public class Depot : Place
{
    public List<UserManagementPlace> ltManagementUserID { get; set; }
    public override string CheckRole(int UserID)
    {
        return Role.Check(UserID, Constants.TabID.KHO, Role.ROLE_KHO_CRUD);
    }
    public override string GetDisplayName() => "Kho";

    public override string SetData(int AccountID)
    {
        string msg = base.SetData(AccountID);
        if (msg.Length > 0) return msg;

        foreach (var item in ltManagementUserID)
            item.PlaceID = PlaceID;

        return string.Empty;
    }

    public override string InsertUpdate(DBM dbm, out Place o)
    {
        string msg = base.InsertUpdate(dbm, out o);
        if (msg.Length > 0) return msg;

        msg = UserManagementPlace.InsertDataType(dbm, ltManagementUserID, o.PlaceID);
        if (msg.Length > 0) return msg;

        return msg;
    }

    public override string GetLogMessageInsertUpdate() => PlaceID == 0 ? "Thêm Kho để tài sản" : "Sửa Kho để tài sản";

    public override string Validate()
    {
        List<int> ltUserNotAuthorized = new List<int>();

        foreach (var item in ltManagementUserID)
        {
            string msg = Role.Check(item.AccountUserID, Constants.TabID.KHOVP, Role.ROLE_KHOVP_IsVisitPage, out var isRole);
            if (msg.Length > 0) return msg;

            if (!isRole) ltUserNotAuthorized.Add(item.AccountUserID);
        }

        return ltUserNotAuthorized.Any() ? $"UserID không có quyền quản lý kho {string.Join(",", ltUserNotAuthorized)}" : string.Empty;
    }


}

public class PlaceDetail
{
    public string PlaceName { get; set; }
    public string PlaceCode { get; set; }
    public string PlaceNameParent { get; set; }
    public string DiagramLocation { get; set; }
    public string DiagramUrl { get; set; }
    public string PlaceDescription { get; set; }

    public static string ViewDetailByPlaceID(int PlaceID, int AccountID, out PlaceDetail placeDetail)
    {
        return DBM.GetOne("usp_Place_ViewDetailByPlaceID", new { PlaceID, AccountID }, out placeDetail);
    }
}
