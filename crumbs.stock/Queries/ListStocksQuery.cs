using MediatR;
using crumbs.stock.Models;

namespace crumbs.stock.Queries
{
    /// <summary>
    /// Query to retrieve a list of stocks for a subscription.
    /// </summary>
    public class ListStocksQuery : IRequest<IEnumerable<StockReadModel>>
    {
        public Guid SubscriptionId { get; }
        public Guid UserId { get; }

        public ListStocksQuery(Guid subscriptionId, Guid userId)
        {
            SubscriptionId = subscriptionId;
            UserId = userId;
        }
    }
}
