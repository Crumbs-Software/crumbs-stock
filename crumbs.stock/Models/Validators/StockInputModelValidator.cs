using FluentValidation;

namespace crumbs.stock.Models.Validators
{
    /// <summary>
    /// Validator for StockInputModel.
    /// </summary>
    public class StockInputModelValidator : AbstractValidator<StockInputModel>
    {
        public StockInputModelValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Product ID must be greater than 0.");

            RuleFor(x => x.WarehouseId)
                .GreaterThan(0).WithMessage("Warehouse ID must be greater than 0.");
        }
    }
}
