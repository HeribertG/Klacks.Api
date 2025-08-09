using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Services.Clients;

public class ClientFilterService : IClientFilterService
{
    public IQueryable<Client> ApplyGenderFilter(IQueryable<Client> query, int[] genderTypes, bool? legalEntity)
    {
        if ((legalEntity == null || (legalEntity.Value == false)) && genderTypes.Length == 0)
        {
            return query.Where(co => co.LegalEntity == false);
        }

        if (legalEntity == null || (legalEntity.Value == false))
        {
            return query.Where(co => genderTypes.Any(y => y == ((int)co.Gender)));
        }
        else if (legalEntity != null && legalEntity.Value)
        {
            return query.Where(co => genderTypes.Any(y => y == ((int)co.Gender)) || co.LegalEntity);
        }

        return query;
    }

    public IQueryable<Client> ApplyAddressTypeFilter(IQueryable<Client> query, int[] addressTypes)
    {
        if (addressTypes.Length < 3)
        {
            return query.Where(x => x.Addresses.Count == 0 || x.Addresses.Any(y => addressTypes.Contains((int)y.Type)));
        }

        return query;
    }

    public IQueryable<Client> ApplyAnnotationFilter(IQueryable<Client> query, bool? hasAnnotation)
    {
        if (hasAnnotation != null && hasAnnotation.Value)
        {
            return query.Where(co => co.Annotations.Count > 0);
        }

        return query;
    }

    public IQueryable<Client> ApplyStateOrCountryFilter(IQueryable<Client> query, List<StateCountryToken> stateTokens, List<CountryResource> countries)
    {
        if (stateTokens?.Any() == true || countries?.Any() == true)
        {
            var stateNames = stateTokens?.Where(x => x.Select).Select(x => x.State.ToLower()).ToList() ?? new List<string>();
            var countryNames = countries?.Where(x => x.Select).Select(x => x.Name.De.ToLower()).ToList() ?? new List<string>();

            if (stateNames.Any() && !countryNames.Any())
            {
                return query.Where(co => co.Addresses.Any(ad => stateNames.Contains(ad.State.ToLower())));
            }
            else if (!stateNames.Any() && countryNames.Any())
            {
                return query.Where(co => co.Addresses.Any(ad => countryNames.Contains(ad.Country.ToLower())));
            }
            else if (stateNames.Any() && countryNames.Any())
            {
                return query.Where(co => co.Addresses.Any(ad => 
                    stateNames.Contains(ad.State.ToLower()) || 
                    countryNames.Contains(ad.Country.ToLower())));
            }
        }

        return query;
    }
}