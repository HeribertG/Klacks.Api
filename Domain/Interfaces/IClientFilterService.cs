using Klacks.Api.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Domain.Interfaces;

public interface IClientFilterService
{
    IQueryable<Client> ApplyGenderFilter(IQueryable<Client> query, int[] genderTypes, bool? legalEntity);
    IQueryable<Client> ApplyAddressTypeFilter(IQueryable<Client> query, int[] addressTypes);
    IQueryable<Client> ApplyAnnotationFilter(IQueryable<Client> query, bool? hasAnnotation);
    IQueryable<Client> ApplyStateOrCountryFilter(IQueryable<Client> query, List<StateCountryToken> stateTokens, List<CountryResource> countries);
}