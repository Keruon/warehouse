using AutoMapper;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.MappingProfiles;

public class WarehouseMappingProfile : Profile
{
    public WarehouseMappingProfile()
    {
        CreateMap<WarehouseArea, AreaResponse>();
        CreateMap<CreateAreaRequest, WarehouseArea>();
        CreateMap<UpdateAreaRequest, WarehouseArea>();

        CreateMap<WarehouseShelf, ShelfResponse>();
        CreateMap<CreateShelfRequest, WarehouseShelf>();
        CreateMap<UpdateShelfRequest, WarehouseShelf>();

        CreateMap<WarehouseLocation, LocationResponse>();
        CreateMap<CreateLocationRequest, WarehouseLocation>();
        CreateMap<UpdateLocationRequest, WarehouseLocation>();
    }
}
