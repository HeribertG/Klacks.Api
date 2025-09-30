using AutoMapper;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.AutoMapper;

public class FilterCriteriaMappingProfile : Profile
{
    public FilterCriteriaMappingProfile()
    {
        CreateMap<BaseFilter, BaseCriteria>()
            .ForMember(dest => dest.FirstItemOnLastPage, opt => opt.MapFrom(src => src.FirstItemOnLastPage))
            .ForMember(dest => dest.IsNextPage, opt => opt.MapFrom(src => src.IsNextPage))
            .ForMember(dest => dest.IsPreviousPage, opt => opt.MapFrom(src => src.IsPreviousPage))
            .ForMember(dest => dest.NumberOfItemsPerPage, opt => opt.MapFrom(src => src.NumberOfItemsPerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.RequiredPage, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder));

        CreateMap<FilterResource, ClientSearchCriteria>()
            .ForMember(dest => dest.FirstItemOnLastPage, opt => opt.MapFrom(src => src.FirstItemOnLastPage))
            .ForMember(dest => dest.IsNextPage, opt => opt.MapFrom(src => src.IsNextPage))
            .ForMember(dest => dest.IsPreviousPage, opt => opt.MapFrom(src => src.IsPreviousPage))
            .ForMember(dest => dest.NumberOfItemsPerPage, opt => opt.MapFrom(src => src.NumberOfItemsPerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.RequiredPage, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder))
            .ForMember(dest => dest.SearchString, opt => opt.MapFrom(src => src.SearchString))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => 
                src.Male == true ? 1 : 
                src.Female == true ? 0 : 
                src.LegalEntity == true ? 3 : (int?)null))
            .ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => 
                src.HomeAddress == true ? 1 : 
                src.CompanyAddress == true ? 2 : 
                src.InvoiceAddress == true ? 3 : (int?)null))
            .ForMember(dest => dest.HasAnnotation, opt => opt.MapFrom(src => src.HasAnnotation))
            .ForMember(dest => dest.IsActiveMember, opt => opt.MapFrom(src => src.ActiveMembership))
            .ForMember(dest => dest.IsFormerMember, opt => opt.MapFrom(src => src.FormerMembership))
            .ForMember(dest => dest.IsFutureMember, opt => opt.MapFrom(src => src.FutureMembership))
            .ForMember(dest => dest.MembershipStartDate, opt => opt.MapFrom(src => src.ScopeFrom.HasValue ? DateOnly.FromDateTime(src.ScopeFrom.Value) : (DateOnly?)null))
            .ForMember(dest => dest.MembershipEndDate, opt => opt.MapFrom(src => src.ScopeUntil.HasValue ? DateOnly.FromDateTime(src.ScopeUntil.Value) : (DateOnly?)null))
            .ForMember(dest => dest.StateOrCountryCode, opt => opt.Ignore())
            .ForMember(dest => dest.AnnotationText, opt => opt.Ignore())
            .ForMember(dest => dest.MembershipYear, opt => opt.Ignore())
            .ForMember(dest => dest.BreaksYear, opt => opt.Ignore());

        CreateMap<FilterResource, ClientFilter>()
            .ForMember(dest => dest.FilteredCantons, opt => opt.MapFrom(src => 
                src.List.Select(t => t.State).ToList()))
            .ForMember(dest => dest.Countries, opt => opt.MapFrom(src => src.Countries.Select(c => c.Abbreviation).ToList()))
            .ForMember(dest => dest.FilteredStateToken, opt => opt.MapFrom(src => 
                src.List.Select(t => new StateCountryFilter 
                { 
                    Id = t.Id, 
                    Country = t.Country, 
                    State = t.State, 
                    Select = t.Select 
                }).ToList()));

        CreateMap<StateCountryToken, StateCountryFilter>();

        CreateMap<Presentation.DTOs.Filter.BreakFilter, Domain.Models.Filters.BreakFilter>()
            .ForMember(dest => dest.AbsenceIds, opt => opt.MapFrom(src => src.Absences.Where(a => a.Checked).Select(a => a.Id).ToList()));

        CreateMap<Presentation.DTOs.Filter.WorkFilter, Domain.Models.Filters.WorkFilter>();
        CreateMap<Presentation.DTOs.Filter.GroupFilter, Domain.Models.Filters.GroupFilter>();
        CreateMap<Presentation.DTOs.Filter.AbsenceFilter, Domain.Models.Filters.AbsenceFilter>();

        CreateMap<BaseFilter, PaginationParams>()
            .ForMember(dest => dest.PageIndex, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.NumberOfItemsPerPage > 0 ? src.NumberOfItemsPerPage : 20))
            .ForMember(dest => dest.SortBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.IsDescending, opt => opt.MapFrom(src => src.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)));

        CreateMap<PagedResult<ClientSummary>, BaseTruncatedResult>()
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => (src.PageNumber - 1) * src.PageSize + 1));
    }
}