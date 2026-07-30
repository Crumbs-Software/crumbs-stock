using MediatR;
using AutoMapper;
using crumbs.common.DataAccess;
using crumbs.stock.Models;
using flinout.core.entities.inventories;
using flinout.core.applications.Exceptions;
using nesae.core.domain.services.infrastructure;
using Microsoft.EntityFrameworkCore;
using crumbs.products.Repositories;

namespace crumbs.stock.Commands
{
    /// <summary>
    /// Handler for UpsertStockCommand - creates or updates stock amounts.
    /// </summary>
    public class UpsertStockCommandHandler : IRequestHandler<UpsertStockCommand, StockReadModel>
    {
        private readonly IWriteOnlyRepository<StockAmount> _repStock;
        private readonly IUserReadOnlyRepository _repUser;
        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public UpsertStockCommandHandler(
            IWriteOnlyRepository<StockAmount> repStock,
            IUserReadOnlyRepository repUser,
            IMapper mapper,
            DataContext context)
        {
            _repStock = repStock;
            _repUser = repUser;
            _mapper = mapper;
            _context = context;
        }

        public Task<StockReadModel> Handle(UpsertStockCommand command, CancellationToken cancellationToken)
        {
            // 1. Authenticate user
            var user = _repUser.GetUserByIdentity(command.SubscriptionId, command.UserId, asTracking: true)
                ?? throw new NotAuthenticatedException(command.UserId);
            
            // 2. Get subscription
            var subscription = _repUser.GetSubscriptionById(command.SubscriptionId, asTracking: true)
                ?? throw new NotAuthorizedException();

            // 3. Idempotency check - if SourceVersion is provided, check if already processed
            // Skip idempotency for formula ingredients: a single movement product generates
            // multiple stock changes for formulas, and StockProcessedVersion can only track one.
            bool alreadyProcessed = false;
            if (!command.Model.IsFormulaIngredient && !string.IsNullOrEmpty(command.Model.SourceVersion) && !string.IsNullOrEmpty(command.Model.SourceEntityType))
            {
                alreadyProcessed = CheckIfAlreadyProcessed(command.Model.SourceEntityType, command.Model.SourceVersion);
                
                if (alreadyProcessed)
                {
                    // Already processed - return current stock state without applying delta again
                    var existingStock = GetStock(command.SubscriptionId, command.Model.WarehouseId, command.Model.ProductId);
                    if (existingStock != null)
                    {
                        return Task.FromResult(_mapper.Map<StockReadModel>(existingStock));
                    }
                    // If stock doesn't exist but was already processed, something is wrong - fall through to normal processing
                }
            }

            // 3.5. Adjustment guard: If a recent adjustment exists for this product+warehouse,
            // skip stock updates from old movement product modifications.
            // After an adjustment (physical count), retroactive changes to previously processed
            // movements should not alter stock — the adjustment is the ground truth.
            if (!command.Model.IsFormulaIngredient
                && command.Model.SourceEntityType == "WarehouseMovementProduct"
                && !string.IsNullOrEmpty(command.Model.SourceEntityId)
                && !string.IsNullOrEmpty(command.Model.SourceVersion))
            {
                if (HasRecentAdjustmentGuard(command.Model))
                {
                    // A recent adjustment exists — skip stock delta but mark as processed
                    MarkSourceAsProcessedWithoutDelta(command.Model);
                    _context.SaveChanges();

                    var guardedStock = GetStock(command.SubscriptionId, command.Model.WarehouseId, command.Model.ProductId);
                    if (guardedStock != null)
                    {
                        return Task.FromResult(_mapper.Map<StockReadModel>(guardedStock));
                    }
                    return Task.FromResult(new StockReadModel());
                }
            }

            // 4. Get Product
            var product = _context.Products
                .FirstOrDefault(x => x.ProductId == command.Model.ProductId && x.EntitySubscription.SubscriptionId == command.SubscriptionId)
                ?? throw new KeyNotFoundException($"Product {command.Model.ProductId} not found");

            // 5. Get Warehouse
            var warehouse = _context.Warehouses
                .Include(w => w.Place)
                .ThenInclude(p => p.EntitySubscription)
                .FirstOrDefault(x => x.WarehouseId == command.Model.WarehouseId);
            
            if (warehouse == null || warehouse.Place.EntitySubscription.SubscriptionId != command.SubscriptionId)
            {
                throw new KeyNotFoundException($"Warehouse {command.Model.WarehouseId} not found");
            }

            // 6. Get or create stock entity
            var entity = GetStock(command.SubscriptionId, command.Model.WarehouseId, command.Model.ProductId);
            
            if (entity == null)
            {
                entity = new StockAmount
                {
                    EntitySubscription = subscription,
                    Product = product,
                    Warehouse = warehouse,
                    CurrentQuantity = command.Model.Quantity,
                    OnTransitQuantity = command.Model.OnTransitQuantity,
                    LastUpdatedBy = user
                };
                _repStock.Insert(entity);
            }
            else
            {
                entity.CurrentQuantity += command.Model.Quantity;
                entity.OnTransitQuantity += command.Model.OnTransitQuantity;
                entity.LastUpdatedBy = user;
                _repStock.Update(entity);
            }
            
            // 7. Update tracking fields on source entity
            // Skip for formula ingredients - only the main product stock change should update tracking
            if (!command.Model.IsFormulaIngredient && !string.IsNullOrEmpty(command.Model.SourceVersion) && !string.IsNullOrEmpty(command.Model.SourceEntityType))
            {
                UpdateSourceEntityTrackingFields(
                    command.Model.SourceEntityType,
                    command.Model.SourceEntityId,
                    command.Model.SourceVersion,
                    command.Model.Quantity,
                    command.Model.WarehouseId,
                    command.Model.IsCompensation);
            }
            
            _repStock.Save();

            // 8. Reload entity with navigations for mapping
            var reloadedEntity = _context.Stocks
                .Include(s => s.Warehouse).ThenInclude(w => w.Place)
                .Include(s => s.Product).ThenInclude(p => p.ProductMeasurementUnit)
                .FirstOrDefault(s => s.Id == entity.Id);

            return Task.FromResult(_mapper.Map<StockReadModel>(reloadedEntity));
        }

        private StockAmount? GetStock(Guid subscriptionId, int warehouseId, long productId)
        {
            return _context.Stocks
                .Include(s => s.Warehouse).ThenInclude(w => w.Place)
                .Include(s => s.Product)
                .FirstOrDefault(x => x.Warehouse.WarehouseId == warehouseId
                    && x.Product.ProductId == productId
                    && x.EntitySubscription.SubscriptionId == subscriptionId);
        }

        /// <summary>
        /// Checks if a source entity version has already been processed for stock updates.
        /// </summary>
        private bool CheckIfAlreadyProcessed(string sourceEntityType, string sourceVersion)
        {
            if (sourceEntityType == "WarehouseMovementProduct")
            {
                var entity = _context.WarehouseMovementProducts
                    .FirstOrDefault(x => x.Version == sourceVersion);
                
                return entity?.StockProcessedVersion == sourceVersion;
            }
            else if (sourceEntityType == "WarehouseMovementAdjustmentProduct")
            {
                var entity = _context.WarehouseMovementAdjustmentProducts
                    .FirstOrDefault(x => x.Version == sourceVersion);
                
                return entity?.StockProcessedVersion == sourceVersion;
            }
            
            return false;
        }

        /// <summary>
        /// Updates the tracking fields on the source entity after successfully processing stock update.
        /// For compensation/reversal: Sets AppliedStockDelta to 0 (signals reversal complete)
        /// For normal apply: Sets AppliedStockDelta to intended delta and AppliedStockWarehouseId to current warehouse
        /// </summary>
        private void UpdateSourceEntityTrackingFields(string sourceEntityType, string? sourceEntityId, string sourceVersion, decimal deltaApplied, int warehouseId, bool isCompensation)
        {
            if (sourceEntityType == "WarehouseMovementProduct" && !string.IsNullOrEmpty(sourceEntityId))
            {
                if (int.TryParse(sourceEntityId, out int entityId))
                {
                    var entity = _context.WarehouseMovementProducts
                        .Include(x => x.Product)
                        .FirstOrDefault(x => x.WarehouseMovementProductId == entityId && x.Version == sourceVersion);
                    
                    if (entity != null)
                    {
                        if (isCompensation)
                        {
                            // Compensation/reversal: set delta to 0 to signal reversal is complete
                            // Keep the old warehouse ID until the apply phase updates it
                            entity.AppliedStockDelta = 0;
                            entity.StockProcessedDateUtc = DateTime.UtcNow;
                            // Note: Don't update StockProcessedVersion until final apply
                        }
                        else
                        {
                            // Normal apply: set to intended delta and update warehouse
                            decimal intendedDelta = CalculateIntendedDelta(entity);
                            entity.AppliedStockDelta = intendedDelta;
                            entity.AppliedStockWarehouseId = warehouseId;
                            entity.StockProcessedVersion = sourceVersion;
                            entity.StockProcessedDateUtc = DateTime.UtcNow;
                        }
                    }
                }
            }
            else if (sourceEntityType == "WarehouseMovementAdjustmentProduct" && !string.IsNullOrEmpty(sourceEntityId))
            {
                if (long.TryParse(sourceEntityId, out long entityId))
                {
                    var entity = _context.WarehouseMovementAdjustmentProducts
                        .Include(x => x.Warehouse)
                        .FirstOrDefault(x => x.Id == entityId && x.Version == sourceVersion);
                    
                    if (entity != null)
                    {
                        if (isCompensation)
                        {
                            // Compensation/reversal: set delta to 0 to signal reversal is complete
                            entity.AppliedStockDelta = 0;
                            entity.StockProcessedDateUtc = DateTime.UtcNow;
                        }
                        else
                        {
                            // For adjustments, the intended delta is the Difference
                            decimal intendedDelta = entity.Quantity - entity.PreviousQuantity;
                            entity.AppliedStockDelta = intendedDelta;
                            entity.AppliedStockWarehouseId = warehouseId;
                            entity.StockProcessedVersion = sourceVersion;
                            entity.StockProcessedDateUtc = DateTime.UtcNow;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the intended stock delta for a movement product based on its flags and quantity.
        /// </summary>
        private decimal CalculateIntendedDelta(flinout.core.entities.inventories.WarehouseMovementProduct entity)
        {
            bool isInput = entity.ForInputs;
            bool isOutput = entity.ForOutputs || entity.ForInventoryTransfer;
            bool isTransfer = entity.ForInventoryTransfer;
            
            // Only calculate delta if inventory flags are set
            if (isInput || isOutput || isTransfer)
            {
                if (isTransfer)
                {
                    return -1 * entity.Qantity;
                }
                else if (isInput)
                {
                    return entity.Qantity;
                }
                else if (isOutput)
                {
                    return -1 * entity.Qantity;
                }
            }
            
            return 0;
        }

        /// <summary>
        /// Checks if there is a recent adjustment for the same product+warehouse that should
        /// prevent stock updates from modifications to an old movement product.
        /// Returns true if the movement product was already processed (update, not first-time)
        /// AND a newer adjustment exists for the same product+warehouse.
        /// </summary>
        private bool HasRecentAdjustmentGuard(StockInputModel model)
        {
            if (!int.TryParse(model.SourceEntityId, out int mpId))
                return false;

            var sourceProduct = _context.WarehouseMovementProducts
                .FirstOrDefault(x => x.WarehouseMovementProductId == mpId);

            if (sourceProduct == null || !sourceProduct.StockProcessedDateUtc.HasValue)
                return false; // First-time processing — no guard needed (new movements go through)

            // Check if there's a newer adjustment for the same product+warehouse
            return _context.WarehouseMovementAdjustmentProducts
                .Any(x => x.Product.ProductId == model.ProductId
                    && x.Warehouse.WarehouseId == model.WarehouseId
                    && x.WarehouseMovement != null
                    && !x.WarehouseMovement.Deleted
                    && x.WarehouseMovement.CreatedDateUTC > sourceProduct.StockProcessedDateUtc.Value);
        }

        /// <summary>
        /// Marks the source movement product as processed without changing AppliedStockDelta
        /// or AppliedStockWarehouseId. This ensures:
        /// - Version == StockProcessedVersion (IsProcessed = true), preventing re-processing
        /// - AppliedStockDelta stays at the previously applied value (accurate tracking)
        /// </summary>
        private void MarkSourceAsProcessedWithoutDelta(StockInputModel model)
        {
            if (!int.TryParse(model.SourceEntityId, out int mpId))
                return;

            var entity = _context.WarehouseMovementProducts
                .FirstOrDefault(x => x.WarehouseMovementProductId == mpId && x.Version == model.SourceVersion);

            if (entity != null)
            {
                entity.StockProcessedVersion = model.SourceVersion;
                entity.StockProcessedDateUtc = DateTime.UtcNow;
                // Do NOT update AppliedStockDelta or AppliedStockWarehouseId —
                // keep what was actually applied to stock
            }
        }
    }
}
