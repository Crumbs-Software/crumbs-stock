using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using crumbs.stock.Commands;
using crumbs.stock.Queries;
using crumbs.stock.Models;
using crumbs.stock.Extensions;

namespace crumbs.stock.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StocksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StocksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lists all stocks for the current subscription.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List()
        {
            var userId = User.GetUserId();
            var subscriptionId = User.GetSubscriptionId();
            var query = new ListStocksQuery(subscriptionId, userId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Upserts (creates or updates) a stock amount.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Upsert([FromBody] StockInputModel model)
        {
            var userId = User.GetUserId();
            var subscriptionId = User.GetSubscriptionId();
            var command = new UpsertStockCommand(model, subscriptionId, userId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
