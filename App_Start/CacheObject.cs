using ASM_API.App_Start.AssetInventory;
using ASM_API.App_Start.InventoryStore;
using ASM_API.App_Start.ItemProposalForm;
using BSS;
using System;
using System.Collections;

static public class CacheObject
{
    static Hashtable ht = new Hashtable();

    static public string GetAssetIDbyGUID(Guid guid, out long AssetID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.Asset, out AssetID);
    }
    static public string GetProposalFormbyGUID(Guid guid, out long ProposalFormID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.ProposalForm, out ProposalFormID);
    }
    static public string GetItemProposalFormbyGUID(Guid guid, out long ProposalFormID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.ItemProposalForm, out ProposalFormID);
    }
    static public string GetAssetInventoryByGUID(Guid guid, out long InventoryID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.AssetInventory, out InventoryID);
    }
    static public string GetIssueIDbyGUID(Guid guid, out long IssueID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.Issue, out IssueID);
    }
    static public string GetUserIDbyGUID(string guid, out long UserID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.User, out UserID);
    }
    static public string GetUserIDbyGUID(Guid guid, out long UserID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.User, out UserID);
    }
    static public string GetPlaceIDByGUID(Guid guid, out long placeID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.Place, out placeID);
    }
    static public string GetDiagramIDByGUID(Guid guid, out long diagramID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.Diagram, out diagramID);
    }
    static public string GetItemIDbyGUID(Guid guid, out long itemID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.Item, out itemID);
    }
    static public string GetStoreIDbyGUID(Guid guid, out long storeID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.Store, out storeID);
    }
    static public string GetInventoryStoreIDbyGUID(Guid guid, out long inventoryStoreID)
    {
        return GetIDbyGUID(guid, Constants.ObjectType.InventoryStore, out inventoryStoreID);
    }

    static public string GetIDbyGUID(string guid, int ot, out long id)
    {
        id = 0;

        Guid og;
        string msg = Convertor.ObjectToGuid(guid, out og);
        if (msg.Length > 0) return msg;
        if (og == Guid.Empty) return "";

        return GetIDbyGUID(og, ot, out id);
    }

    static public string GetIDbyGUID(Guid guid, int ot, out long id)
    {
        id = 0;
        if (guid == Guid.Empty) return "";

        if (ht.ContainsKey(guid)) id = (long)ht[guid];
        else
        {
            string msg = "";
            switch (ot)
            {
                case Constants.ObjectType.Asset: msg = Asset.GetOneByGuid(guid, out id); break;
                case Constants.ObjectType.User: msg = WebAPI.User.GetOneByGuid(guid, out id); break;
                case Constants.ObjectType.Issue: msg = Issue.GetOneByGuid(guid, out id); break;
                case Constants.ObjectType.ProposalForm: msg = ProposalForm.GetOneObjectGuid(guid, out id); break;
                case Constants.ObjectType.AssetInventory: msg = AssetInventory.GetOneObjectGuid(guid, out id); break;
                case Constants.ObjectType.Place: msg = Place.GetOneObjectGuid(guid, out id); break;
                case Constants.ObjectType.Diagram: msg = Diagram.GetOneObjectGuid(guid, out id); break;
                case Constants.ObjectType.Item: msg = Item.GetOneObjectGuid(guid, out id); break;
                case Constants.ObjectType.Store: msg = ItemImportReceipt.GetOneObjectGuid(guid, out id); break;
                case Constants.ObjectType.ItemProposalForm: msg = ItemProposalForm.GetOneObjectGuid(guid, out id); break;
                case Constants.ObjectType.InventoryStore: msg = InventoryStore.GetOneObjectGuid(guid, out id); break;
            }

            if (msg.Length > 0) return msg;

            if (id != 0 && !ht.ContainsKey(guid)) ht.Add(guid, id);
            else BSS.Log.WriteErrorLog("GetIDbyGUID(guid, out id)", new { guid, ot, msg });
        }
        return "";
    }
}