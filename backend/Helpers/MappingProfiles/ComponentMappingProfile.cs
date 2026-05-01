using AutoMapper;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.MappingProfiles;

public class ComponentMappingProfile : Profile
{
    public ComponentMappingProfile()
    {
        CreateMap<Component, ComponentResponse>();
        CreateMap<CreateComponentRequest, Component>();
        CreateMap<UpdateComponentRequest, Component>();

        CreateMap<ComponentType, ComponentTypeResponse>();
        CreateMap<CreateComponentTypeRequest, ComponentType>();
        CreateMap<UpdateComponentTypeRequest, ComponentType>();

        CreateMap<ComponentCategory, ComponentCategoryResponse>();
        CreateMap<CreateComponentCategoryRequest, ComponentCategory>();
        CreateMap<UpdateComponentCategoryRequest, ComponentCategory>();

        CreateMap<Supplier, SupplierResponse>();
        CreateMap<CreateSupplierRequest, Supplier>();
        CreateMap<UpdateSupplierRequest, Supplier>();
    }
}
