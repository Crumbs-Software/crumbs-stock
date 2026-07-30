using MediatR;
using Microsoft.AspNetCore.Mvc;
using crumbs.stock.Commands;
using crumbs.stock.Models;

namespace crumbs.stock.Controllers
{
    /// <summary>
    /// Controller for stock amount operations using API key authentication.
    /// This endpoint is used by external systems.
    /// </summary>
    [Route("api/StockAmount")]
    public class StockAmountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockAmountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Creates or updates a stock amount.
        /// Requires X-ApiKey-Header for authentication.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> PostStockAmount([FromBody] StockAmountInputModel stockAmount)
        {
            // Validate the custom header
            if (!Request.Headers.TryGetValue("X-ApiKey-Header", out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
            {
                return BadRequest("Missing or invalid X-Custom-Header.");
            }
            if (headerValue != "06a70887-a938-42e4-91b8-5155f25db098")
            {
                return BadRequest("Invalid api key.");
            }

            var inputModel = new StockInputModel
            {
                ProductId = stockAmount.Product,
                WarehouseId = stockAmount.Warehouse,
                Quantity = stockAmount.Quantity,
                OnTransitQuantity = stockAmount.OnTransitQuantity,
                //Idempotency fields
                SourceEntityType = stockAmount.SourceEntityType,
                SourceEntityId = stockAmount.SourceEntityId,
                SourceVersion = stockAmount.SourceVersion,
                IdempotencyKey = $"{stockAmount.SourceEntityType}_{stockAmount.SourceVersion}_{stockAmount.WorkflowInstanceId}",
                IsCompensation = stockAmount.IsCompensation,
                IsFormulaIngredient = stockAmount.IsFormulaIngredient
            };

            var command = new UpsertStockCommand(inputModel, stockAmount.SubscriptionId, stockAmount.UserId);
            await _mediator.Send(command);
            return Ok();
        }
    }

    /// <summary>
    /// Input model for the StockAmount API endpoint (external system compatibility).
    /// </summary>
    public class StockAmountInputModel
    {
        public int Id { get; set; }
        public decimal Quantity { get; set; }
        public decimal OnTransitQuantity { get; set; }
        public long Product { get; set; }
        public int Warehouse { get; set; }
        public int PlaceId { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid UserId { get; set; }
        
        //Idempotency and tracking fields
        public string? SourceEntityType { get; set; }
        public string? SourceEntityId { get; set; }
        public string? SourceVersion { get; set; }
        public string? WorkflowInstanceId { get; set; }
        public bool IsCompensation { get; set; }
        public bool IsFormulaIngredient { get; set; }
    }
}
