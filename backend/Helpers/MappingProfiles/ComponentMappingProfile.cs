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
    }
}
