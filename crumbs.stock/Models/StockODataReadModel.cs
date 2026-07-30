namespace crumbs.stock.Models
{
    /// <summary>
    /// OData-compatible read model for stock queries.
    /// Supports $filter, $select, $orderby OData operations.
    /// </summary>
    public class StockODataReadModel
    {
        public int Id { get; set; }
        public int PlaceId { get; set; }
        public int WarehouseId { get; set; }
        public string Place { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public bool IsWarehouseRawMaterial { get; set; }
        public bool IsWarehouseFinishGood { get; set; }
        public bool IsWarehouseWip { get; set; }
        public string Product { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public bool IsProductRawMaterial { get; set; }
        public bool IsProductFinishGood { get; set; }
        public bool IsProductWip { get; set; }
        public bool IsProductActive { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public string CurrentQuantityAction { get; set; } = string.Empty;
    }
}
