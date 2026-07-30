using AutoMapper;
using flinout.core.entities.inventories;

namespace crumbs.stock.Models
{
    /// <summary>
    /// AutoMapper profile for Stock entity mappings.
    /// </summary>
    public class StockProfile : Profile
    {
        public StockProfile()
        {
            // StockAmount -> StockReadModel
            CreateMap<StockAmount, StockReadModel>()
                .ForMember(dest => dest.Place, opt => opt.MapFrom(src => src.Warehouse.Place.Name))
                .ForMember(dest => dest.Warehouse, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.IsWarehouseRawMaterial, opt => opt.MapFrom(src => src.Warehouse.IsRawMaterial))
                .ForMember(dest => dest.IsWarehouseFinishGood, opt => opt.MapFrom(src => src.Warehouse.IsFinishGood))
                .ForMember(dest => dest.IsWarehouseWip, opt => opt.MapFrom(src => src.Warehouse.IsWIP))
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Product.ProductId))
                .ForMember(dest => dest.IsProductRawMaterial, opt => opt.MapFrom(src => src.Product.IsRawMaterial))
                .ForMember(dest => dest.IsProductFinishGood, opt => opt.MapFrom(src => src.Product.IsFinishGood))
                .ForMember(dest => dest.IsProductWip, opt => opt.MapFrom(src => src.Product.IsWIP))
                .ForMember(dest => dest.IsProductActive, opt => opt.MapFrom(src => src.Product.Active))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product.Code ?? ""))
                .ForMember(dest => dest.UnitOfMeasure, opt => opt.MapFrom(src => src.Product.ProductMeasurementUnit != null ? src.Product.ProductMeasurementUnit.Name ?? "" : ""))
                .ForMember(dest => dest.CurrentQuantityAction, opt => opt.MapFrom(src => src.CurrentQuantity > 0 ? "statusgreen" : "statusred"));

            // StockInputModel -> StockAmount (partial mapping, additional setup in handler)
            CreateMap<StockInputModel, StockAmount>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EntitySubscription, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.WarehouseId))
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.CurrentQuantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.OnTransitQuantity, opt => opt.MapFrom(src => src.OnTransitQuantity));
        }
    }
}
