DECLARE @DefaultDate DATE = '1900-01-01';
DECLARE @TextSearch NVARCHAR(MAX) = N'',
		@ItemTypeID INT = 0,
		@ItemStatusID INT = 0,
		@ItemDateFrom DATE = @DefaultDate,
		@ItemDateTo DATE = @DefaultDate,
		@AccountID INT,
		@UserID INT
SELECT   
		item.ItemID,
		item.ObjectGuid,
		item.ItemTypeID,
		assetType.AssetTypeName AS ItemTypeName,
		item.ItemCode,
		item.ItemName,
		item.ItemUnitStatusID,
		itemUnit.ItemUnitName,
		item.WarningThreshold,
		item.ItemStatusID,
		itemStatus.ItemStatusName,
		item.UserIDCreate,
		item.UserIDApprove,
		item.UserIDManager
FROM dbo.Item item
	LEFT JOIN dbo.AssetType assetType
		ON item.ItemTypeID = assetType.AssetTypeID
		AND assetType.AssetTypeGroupID = 2
	LEFT JOIN dbo.ItemUnit itemUnit
		ON item.ItemUnitStatusID = itemUnit.ItemUnitID
	LEFT JOIN dbo.ItemStatus itemStatus
		ON item.ItemStatusID = itemStatus.ItemStatusID

WHERE 
	  (
		@TextSearch = ''
		OR item.ItemCode LIKE N'%' + @TextSearch + '%'
		OR item.ItemNameNoMark LIKE N'%' + dbo.fnRemoveMarkVietnamese(@TextSearch) + '%' 
	  )
	  AND
	  (
		@ItemTypeID = 0
		OR item.ItemTypeID = @ItemTypeID
	  )
	  AND
	  (
		@ItemStatusID = 0
		OR item.ItemStatusID = @ItemStatusID
	  )
	  AND
	  (
		@ItemDateFrom = @DefaultDate
        OR item.LastUpdate >= @ItemDateFrom
	  )
	  AND
	  (
		@ItemDateTo = @DefaultDate
        OR item.LastUpdate <= @ItemDateFrom
	  )
	  AND item.AccountID = @AccountID
	  AND 
	  (
		item.UserIDCreate = @UserID
		OR item.UserIDApprove = @UserID
	  )
      
