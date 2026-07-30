using MediatR;
using crumbs.stock.Models;

namespace crumbs.stock.Queries
{
    /// <summary>
    /// Query to retrieve stocks as IQueryable for OData support.
    /// </summary>
    public class GetStocksODataQuery : IRequest<IQueryable<StockODataReadModel>>
    {
        public Guid SubscriptionId { get; }
        public Guid UserId { get; }

        public GetStocksODataQuery(Guid subscriptionId, Guid userId)
        {
            SubscriptionId = subscriptionId;
            UserId = userId;
        }
    }
}
