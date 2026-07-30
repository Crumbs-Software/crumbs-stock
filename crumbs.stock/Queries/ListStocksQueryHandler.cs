using MediatR;
using crumbs.stock.Models;
using flinout.core.applications.Exceptions;
using flinout.core.entities.Users;
using flinout.core.entities.Users.Enums;
using Microsoft.EntityFrameworkCore;
using nesae.core.domain.services.infrastructure;

namespace crumbs.stock.Queries
{
    /// <summary>
    /// Handler for ListStocksQuery - retrieves stocks with proper authorization.
    /// </summary>
    public class ListStocksQueryHandler : IRequestHandler<ListStocksQuery, IEnumerable<StockReadModel>>
    {
        private readonly DataContext _context;

        public ListStocksQueryHandler(DataContext context)
        {
            _context = context;
        }

        public Task<IEnumerable<StockReadModel>> Handle(ListStocksQuery query, CancellationToken cancellationToken)
        {
            // 1. Get and validate user with roles
            var user = _context.Users
                .Where(x => x.IdentityUserId == query.UserId && x.IdSubscription == query.SubscriptionId)
                .Include(x => x.UserGroupsRelations)
                    .ThenInclude(x => x.UserGroup)
                        .ThenInclude(x => x.UserGroupsRolesRelations)
                            .ThenInclude(x => x.UserGroupRole)
                .SingleOrDefault();

            if (user == null)
            {
                throw new NotAuthenticatedException(query.UserId);
            }

            // 2. Check authorization
            if (!HasRole(user.UserGroupsRelations, RolesEnum.STOCK_READ) && !user.IsAdminUser)
            {
                throw new NotAuthorizedException(query.UserId, RolesEnum.STOCK_READ);
            }

            // 3. Determine data access level
            bool fullAccess = user.UserGroupsRelations.Any(x => x.UserGroup.FullDataAccess) || user.IsAdminUser;
            List<int> placesAccess = new List<int>();
            
            if (!fullAccess)
            {
                var employee = _context.Persons
                    .Where(x => x.UserInfo != null 
                        && x.UserInfo.IdentityUserId == query.UserId
                        && x.EntitySubscription.SubscriptionId == query.SubscriptionId
                        && x.EmployeeInfo != null)
                    .Include(x => x.EmployeeInfo!.Place)
                    .Select(x => x.EmployeeInfo)
                    .SingleOrDefault();

                if (employee?.Place != null)
                {
                    placesAccess.Add(employee.Place.Id);
                }
            }

            // 4. Get latest stock entries per warehouse/product combination
            var latestStockIds = from stocks in _context.Stocks
                where stocks.EntitySubscription.SubscriptionId == query.SubscriptionId
                    && !stocks.Deleted
                    && !stocks.Product.Deleted
                    && (fullAccess || placesAccess.Contains(stocks.Warehouse.Place.Id))
                group stocks by new { stocks.Warehouse.WarehouseId, stocks.Product.ProductId } into g
                select g.Max(x => x.Id);

            // 5. Query and project to read model
            var result = (from stocks in _context.Stocks
                join ids in latestStockIds on stocks.Id equals ids
                where stocks.EntitySubscription.SubscriptionId == query.SubscriptionId 
                    && stocks.CurrentQuantity != 0
                select new StockReadModel
                {
                    Id = stocks.Id,
                    CurrentQuantity = stocks.CurrentQuantity,
                    Place = stocks.Warehouse.Place.Name,
                    Warehouse = stocks.Warehouse.Name,
                    IsWarehouseRawMaterial = stocks.Warehouse.IsRawMaterial,
                    IsWarehouseFinishGood = stocks.Warehouse.IsFinishGood,
                    IsWarehouseWip = stocks.Warehouse.IsWIP,
                    Product = stocks.Product.Name,
                    ProductId = stocks.Product.ProductId,
                    IsProductRawMaterial = stocks.Product.IsRawMaterial,
                    IsProductFinishGood = stocks.Product.IsFinishGood,
                    IsProductWip = stocks.Product.IsWIP,
                    IsProductActive = stocks.Product.Active,
                    ProductCode = stocks.Product.Code ?? "",
                    UnitOfMeasure = stocks.Product.ProductMeasurementUnit != null ? stocks.Product.ProductMeasurementUnit.Name ?? "" : "",
                    CurrentQuantityAction = stocks.CurrentQuantity > 0 ? "statusgreen" : "statusred"
                }).ToList();

            return Task.FromResult<IEnumerable<StockReadModel>>(result);
        }

        private static bool HasRole(ICollection<UserGroupsRelation> groups, RolesEnum role)
        {
            return groups != null
                && groups.Any(x => x.UserGroup.UserGroupsRolesRelations.Any(y => y.UserGroupRole.UserGroupRoleId == (int)role));
        }
    }
}
