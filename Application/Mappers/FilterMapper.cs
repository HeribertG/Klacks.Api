using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Staffs;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class FilterMapper
{
    public ClientSearchCriteria ToClientSearchCriteria(FilterResource filter)
    {
        return new ClientSearchCriteria
        {
            FirstItemOnLastPage = filter.FirstItemOnLastPage,
            IsNextPage = filter.IsNextPage,
            IsPreviousPage = filter.IsPreviousPage,
            NumberOfItemsPerPage = filter.NumberOfItemsPerPage,
            OrderBy = filter.OrderBy,
            RequiredPage = filter.RequiredPage,
            SortOrder = filter.SortOrder,
            SearchString = filter.SearchString,
            Gender = filter.Male == true ? GenderEnum.Male :
                     filter.Female == true ? GenderEnum.Female :
                     filter.LegalEntity == true ? GenderEnum.LegalEntity : null,
            AddressType = filter.HomeAddress == true ? AddressTypeEnum.Workplace :
                          filter.CompanyAddress == true ? AddressTypeEnum.InvoicingAddress :
                          filter.InvoiceAddress == true ? (AddressTypeEnum)3 : null,
            HasAnnotation = filter.HasAnnotation,
            IsActiveMember = filter.ActiveMembership,
            IsFormerMember = filter.FormerMembership,
            IsFutureMember = filter.FutureMembership,
            MembershipStartDate = filter.ScopeFrom.HasValue
                ? DateOnly.FromDateTime(filter.ScopeFrom.Value)
                : null,
            MembershipEndDate = filter.ScopeUntil.HasValue
                ? DateOnly.FromDateTime(filter.ScopeUntil.Value)
                : null
        };
    }

    public ClientFilter ToClientFilter(FilterResource filter)
    {
        return new ClientFilter
        {
            SearchString = filter.SearchString,
            SearchOnlyByName = filter.SearchOnlyByName,
            ActiveMembership = filter.ActiveMembership,
            FormerMembership = filter.FormerMembership,
            FutureMembership = filter.FutureMembership,
            ScopeFrom = filter.ScopeFrom,
            ScopeUntil = filter.ScopeUntil,
            ClientType = filter.ClientType,
            LegalEntity = filter.LegalEntity,
            Male = filter.Male,
            Female = filter.Female,
            Intersexuality = filter.Intersexuality,
            Employee = filter.Employee,
            ExternEmp = filter.ExternEmp,
            Customer = filter.Customer,
            ShowDeleteEntries = filter.ShowDeleteEntries,
            SelectedGroup = filter.SelectedGroup,
            FilteredCantons = filter.List.Select(t => t.State).ToList(),
            Countries = filter.Countries.Select(c => c.Abbreviation).ToList(),
            FilteredStateToken = filter.List.Select(t => new StateCountryFilter
            {
                Id = t.Id,
                Country = t.Country,
                State = t.State,
                Select = t.Select
            }).ToList(),
            CountriesHaveBeenReadIn = filter.CountriesHaveBeenReadIn,
            Language = filter.Language,
            MacroFilter = filter.MacroFilter,
            HasAnnotation = filter.HasAnnotation,
            CompanyAddress = filter.CompanyAddress,
            HomeAddress = filter.HomeAddress,
            InvoiceAddress = filter.InvoiceAddress,
            IncludeAddress = filter.IncludeAddress,
            ScopeFromFlag = filter.ScopeFromFlag,
            ScopeUntilFlag = filter.ScopeUntilFlag,
            OrderBy = filter.OrderBy,
            SortOrder = filter.SortOrder
        };
    }

    public partial StateCountryFilter ToStateCountryFilter(StateCountryToken token);

    public Domain.Models.Filters.BreakFilter ToBreakFilter(Presentation.DTOs.Filter.BreakFilter filter)
    {
        return new Domain.Models.Filters.BreakFilter
        {
            SearchString = filter.SearchString,
            CurrentYear = filter.CurrentYear,
            OrderBy = filter.OrderBy,
            SortOrder = filter.SortOrder,
            SelectedGroup = filter.SelectedGroup,
            StartRow = filter.StartRow,
            RowCount = filter.RowCount,
            AbsenceIds = filter.Absences?.Where(a => a.Checked).Select(a => a.Id).ToList() ?? [],
            ShowEmployees = filter.ShowEmployees,
            ShowExtern = filter.ShowExtern,
            HoursSortOrder = filter.HoursSortOrder
        };
    }

    public partial Domain.Models.Filters.WorkFilter ToWorkFilter(Presentation.DTOs.Filter.WorkFilter filter);
    public partial Domain.Models.Filters.GroupFilter ToGroupFilter(Presentation.DTOs.Filter.GroupFilter filter);
    public partial Domain.Models.Filters.AbsenceFilter ToAbsenceFilter(Presentation.DTOs.Filter.AbsenceFilter filter);

    public PaginationParams ToPaginationParams(BaseFilter filter)
    {
        return new PaginationParams
        {
            PageIndex = filter.RequiredPage,
            PageSize = filter.NumberOfItemsPerPage > 0 ? filter.NumberOfItemsPerPage : 20,
            SortBy = filter.OrderBy,
            IsDescending = filter.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)
        };
    }

    public BaseTruncatedResult ToBaseTruncatedResult(PagedResult<ClientSummary> pagedResult)
    {
        return new BaseTruncatedResult
        {
            MaxItems = pagedResult.TotalCount,
            MaxPages = pagedResult.TotalPages,
            CurrentPage = pagedResult.PageNumber,
            FirstItemOnPage = (pagedResult.PageNumber - 1) * pagedResult.PageSize + 1
        };
    }
}
