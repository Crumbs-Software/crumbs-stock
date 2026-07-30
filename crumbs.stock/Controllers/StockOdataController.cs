using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using crumbs.stock.Models;
using crumbs.stock.Queries;
using crumbs.stock.Extensions;

namespace crumbs.stock.Controllers
{
    /// <summary>
    /// OData-enabled controller for querying stock data.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class StockOdataController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockOdataController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gets stocks with OData query support ($filter, $select, $orderby, etc.).
        /// </summary>
        [HttpGet]
        [EnableQuery]
        public async Task<IQueryable<StockODataReadModel>> Get()
        {
            var userId = User.GetUserId();
            var subscriptionId = User.GetSubscriptionId();
            var query = new GetStocksODataQuery(subscriptionId, userId);
            return await _mediator.Send(query);
        }
    }
}
