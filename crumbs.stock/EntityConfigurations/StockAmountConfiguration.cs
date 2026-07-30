using flinout.core.entities.inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace crumbs.stock.EntityConfigurations
{
    /// <summary>
    /// Entity Framework configuration for StockAmount entity.
    /// This configuration is auto-discovered by DataContext via ApplyConfigurationsFromAssembly.
    /// </summary>
    public class StockAmountConfiguration : IEntityTypeConfiguration<StockAmount>
    {
        public void Configure(EntityTypeBuilder<StockAmount> builder)
        {
            builder.ToTable("StockAmounts");
            
            builder.HasKey(a => a.Id);

            builder.Property(x => x.CurrentQuantity)
                .IsConcurrencyToken() // Handle concurrency
                .HasColumnType("decimal(18,4)");
            
            builder.Property(x => x.OnTransitQuantity)
                .HasColumnType("decimal(18,4)");

            // Relationships
            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId);

            // Unique constraint for WarehouseId and ProductId
            builder.HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();
        }
    }
}
