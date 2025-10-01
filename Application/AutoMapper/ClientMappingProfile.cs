using AutoMapper;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Clients;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.AutoMapper;

public class ClientMappingProfile : Profile
{
    public ClientMappingProfile()
    {
        CreateMap<Client, ClientListItemResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IdNumber))
            .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted));

        CreateMap<TruncatedClient, TruncatedClientResource>()
            .ForMember(dest => dest.Clients, opt => opt.MapFrom(src => src.Clients))
            .ForMember(dest => dest.Editor, opt => opt.MapFrom(src => src.Editor))
            .ForMember(dest => dest.LastChange, opt => opt.MapFrom(src => src.LastChange))
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.MaxItems))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.MaxPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.CurrentPage))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => src.FirstItemOnPage));

        CreateMap<Client, ClientResource>()
            .ForMember(dest => dest.Communications, opt => opt.MapFrom(src => src.Communications))
            .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses))
            .ForMember(dest => dest.Annotations, opt => opt.MapFrom(src => src.Annotations))
            .ForMember(dest => dest.Works, opt => opt.MapFrom(src => src.Works));

        CreateMap<ClientResource, Client>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
            .ForMember(dest => dest.Breaks, opt => opt.Ignore());

        CreateMap<Client, ClientBreakResource>();

        CreateMap<Client, ClientWorkResource>()
            .ForMember(dest => dest.Membership, opt => opt.Ignore())
            .ForMember(dest => dest.NeededRows, opt => opt.Ignore());

        CreateMap<ClientSummary, ClientResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender ?? GenderEnum.Female))
            .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => ParseIdNumber(src.IdNumber)))
            .ForMember(dest => dest.Birthdate, opt => opt.MapFrom(src => src.DateOfBirth.HasValue ? src.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.Annotations, opt => opt.Ignore())
            .ForMember(dest => dest.Communications, opt => opt.Ignore())
            .ForMember(dest => dest.LegalEntity, opt => opt.MapFrom(src => src.Gender == GenderEnum.LegalEntity))
            .ForMember(dest => dest.MaidenName, opt => opt.Ignore())
            .ForMember(dest => dest.Membership, opt => opt.Ignore())
            .ForMember(dest => dest.MembershipId, opt => opt.MapFrom(src => Guid.Empty))
            .ForMember(dest => dest.PasswortResetToken, opt => opt.Ignore())
            .ForMember(dest => dest.SecondName, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => !src.IsActive))
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.Works, opt => opt.Ignore());

        CreateMap<Domain.Models.Results.PagedResult<Client>, TruncatedClient>()
            .ForMember(dest => dest.Clients, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => src.Items.Count() == 0 ? -1 : src.PageNumber * src.PageSize))
            .ForMember(dest => dest.Editor, opt => opt.Ignore())
            .ForMember(dest => dest.LastChange, opt => opt.Ignore());
    }

    private static int ParseIdNumber(string idNumber)
    {
        return int.TryParse(idNumber, out var id) ? id : 0;
    }
}