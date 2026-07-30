namespace crumbs.stock.Models
{
    /// <summary>
    /// Input model for creating or updating stock amounts.
    /// </summary>
    public class StockInputModel
    {
        public int? Id { get; set; }
        public decimal Quantity { get; set; }
        public decimal OnTransitQuantity { get; set; }
        public long ProductId { get; set; }
        public int WarehouseId { get; set; }
        
        //Idempotency and tracking fields
        public string? SourceEntityType { get; set; }
        public string? SourceEntityId { get; set; }
        public string? SourceVersion { get; set; }
        public string? IdempotencyKey { get; set; }
        
        /// <summary>
        /// Indicates this is a compensation/reversal operation (e.g., warehouse change reversal).
        /// When true, AppliedStockDelta will be set to 0 instead of the intended delta.
        /// </summary>
        public bool IsCompensation { get; set; }
        
        /// <summary>
        /// When true, this stock change is for a formula ingredient.
        /// Idempotency check and source entity tracking are skipped.
        /// </summary>
        public bool IsFormulaIngredient { get; set; }
    }
}
