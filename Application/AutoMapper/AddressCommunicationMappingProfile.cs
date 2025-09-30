using AutoMapper;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.AutoMapper;

public class AddressCommunicationMappingProfile : Profile
{
    public AddressCommunicationMappingProfile()
    {
        CreateMap<Address, AddressResource>();

        CreateMap<AddressResource, Address>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        CreateMap<Communication, CommunicationResource>();

        CreateMap<CommunicationResource, Communication>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        CreateMap<CommunicationType, CommunicationTypeResource>();
        CreateMap<CommunicationTypeResource, CommunicationType>();
    }
}