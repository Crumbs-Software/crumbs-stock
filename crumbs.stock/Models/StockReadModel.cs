namespace crumbs.stock.Models
{
    /// <summary>
    /// Read model representing stock information for display.
    /// </summary>
    public class StockReadModel
    {
        public int Id { get; set; }
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
