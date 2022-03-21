using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class UserManagementPlace
{
    public int AccountUserID { get; set; }
    [JsonIgnore]
    public int PlaceID { get; set; }
    [JsonIgnore]
    public DateTime CreateDate { get; set; }
    [JsonIgnore]
    public DateTime LastUpdate { get; set; }
    [JsonIgnore]
    public bool IsDelete { get; set; }

    public static string InsertDataType(DBM dbm, List<UserManagementPlace> userManagementPlaces, int PlaceID)
    {
        string JsonData = JsonConvert.SerializeObject(userManagementPlaces);
        string msg = dbm.SetStoreNameAndParams("usp_UserManagementPlace_InsertByDataType", new { JsonData, @PlaceID });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string GetList(int PlaceID, out List<UserManagementPlace> userManagementPlaces)
    {
        return DBM.GetList("usp_UserManagementPlace_GetList", new { PlaceID }, out userManagementPlaces);
    }

    public static string CheckRoleManagement(int UserID, int PlaceID, out UserManagementPlace userManagementPlace)
    {
        return DBM.GetOne("usp_UserManagementPlace_CheckRoleManagement", new
        {
            UserID,
            PlaceID
        }, out userManagementPlace);
    }


}

public class UserManagementPlaceView : UserManagementPlace
{
    public string UserName { get; set; }

    public static string GetList(int PlaceID, out List<UserManagementPlaceView> userManagementPlaces)
    {
        return DBM.GetList("usp_UserManagementPlace_GetList", new { PlaceID }, out userManagementPlaces);
    }
}