using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using nesae.core.domain.services.infrastructure;

namespace crumbs.stock.Extensions
{
    public static class StartupExtensions
    {
        /// <summary>
        /// Registers all services, handlers, validators, and entity configurations for the Stock module.
        /// This is the main entry point for configuring the Stock module in the DI container.
        /// </summary>
        public static IServiceCollection AddStockModule(this IServiceCollection services)
        {
            // 1. Register EF configurations - REQUIRED
            DataContext.RegisterModuleAssembly(typeof(AssemblyMarker).Assembly);

            // 2. Register MediatR handlers - REQUIRED
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));

            // 3. Auto-register validators - REQUIRED
            services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
            
            return services;
        }

        public static IMapperConfigurationExpression AddStockMappings(this IMapperConfigurationExpression cfg)
        {
            cfg.AddMaps(typeof(AssemblyMarker).Assembly);
            return cfg;
        }

        [System.Obsolete("Use AddStockModule instead - maintained for backward compatibility")]
        public static IServiceCollection StockConfigurations(this IServiceCollection services)
        {
            return AddStockModule(services);
        }
    }
}
