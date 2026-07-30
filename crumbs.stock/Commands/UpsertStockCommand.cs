using MediatR;
using crumbs.stock.Models;

namespace crumbs.stock.Commands
{
    /// <summary>
    /// Command to upsert (create or update) stock amounts.
    /// </summary>
    public class UpsertStockCommand : IRequest<StockReadModel>
    {
        public StockInputModel Model { get; }
        public Guid SubscriptionId { get; }
        public Guid UserId { get; }

        public UpsertStockCommand(StockInputModel model, Guid subscriptionId, Guid userId)
        {
            Model = model;
            SubscriptionId = subscriptionId;
            UserId = userId;
        }
    }
}
