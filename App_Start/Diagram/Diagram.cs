using BSS;
using System;
using System.Collections.Generic;
using System.IO;

public class Diagram
{
    public long DiagramID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int AccountID { get; set; }
    public string DiagramName { get; set; }
    public string DiagramUrl { get; set; }
    public string LastUpdate { get; set; }
    public string CreateDate { get; set; }
    public string FileName { get; set; }
    public string FileExttension
    {
        get
        {
            return Path.GetExtension(FileName);
        }
    }

    public string InsertOrUpdate(DBM dbm, out Diagram diagram)
    {
        diagram = null;

        string msg = dbm.SetStoreNameAndParams("usp_Diagram_InsertOrUpdate",
                    new
                    {
                        DiagramID,
                        AccountID,
                        DiagramName,
                        DiagramUrl
                    });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out diagram);
    }

    public static string GetListByDiagramID(int AccountID, long DiagramID, out List<Diagram> diagramList)
    {
        return DBM.GetList("usp_Diagram_GetByDiagramID", new { AccountID, DiagramID }, out diagramList);
    }

    public static string GetSuggestSearch(string TextSearch, int AccountID, out List<Diagram> diagramList)
    {
        return DBM.GetList("usp_Diagram_SuggestSearch", new { TextSearch, AccountID }, out diagramList);
    }

    public static string DeleteByDiagramIDs(string DiagramIDs)
    {
        return DBM.ExecStore("usp_Diagram_DeleteByDiagramIDs", new { DiagramIDs });
    }

    public static string GetOneObjectGuid(Guid ObjectGuid, out long diagramID)
    {
        diagramID = 0;

        string msg = DBM.GetOne("usp_Diagram_GetByObjectGuid", new { ObjectGuid }, out Diagram diagram);
        if (msg.Length > 0) return msg;

        if (diagram == null) return ("Không tồn tại sơ đồ có ObjectGuid = " + ObjectGuid).ToMessageForUser();
        diagramID = diagram.DiagramID;

        return msg;
    }

    public class DiagramDetail
    {
        public long DiagramID { get; set; }
        public string DiagramName { get; set; }
        public string DiagramUrl { get; set; }
        public long CountAsset { get; set; }
        public int CountPlace { get; set; }

        public static string GetOneByDiagramID(long DiagramID, out DiagramDetail diagramDetail)
        {
            return DBM.GetOne("usp_DiagramDetail_GetByDiagramID", new { DiagramID }, out diagramDetail);
        }
    }

    public class DiagramPlace
    {
        public int PlaceID { get; set; }
        public string PlaceName { get; set; }
        public string DiagramLocation { get; set; }
        public long CountAsset { get; set; }

        public static string GetListByDiagramID(long DiagramID, out List<DiagramPlace> diagramPlaceList)
        {
            return DBM.GetList("usp_Place_GetByDiagramID", new { DiagramID }, out diagramPlaceList);
        }
    }

    public class AssetDetail
    {
        public long AssetID { get; set; }
        public string AssetCode { get; set; }
        public string AssetSerial { get; set; }
        public string AssetModel { get; set; }
        public int UserIDHolding { get; set; }
        public string UserNameHolding { get; set; }
        public string PositionName { get; set; }

        public static string GetAssetDetailByDiagramIDAndPlaceID(long DiagramID, int PlaceID, out List<AssetDetail> assetDetails)
        {
            return DBM.GetList("usp_Diagram_GetAssetDetailByDiagramIDAndPlaceID", new { DiagramID, PlaceID }, out assetDetails);
        }
    }
}

