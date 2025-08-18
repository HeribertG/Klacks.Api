using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientFilterService : IClientFilterService
{
    public IQueryable<Client> ApplyGenderFilter(IQueryable<Client> query, int[] genderTypes, bool? legalEntity)
    {
        if (genderTypes.Length == 0 && (legalEntity == null || !legalEntity.Value))
        {
            return query.Where(co => false);
        }

        if (legalEntity != null && legalEntity.Value)
        {
            if (genderTypes.Length > 0 )
            {
                return query.Where(co => co.LegalEntity == legalEntity.Value && genderTypes.Any(y => y == ((int)co.Gender)));
            }

            return query.Where(co => co.LegalEntity == legalEntity.Value );
        } 

        if (genderTypes.Length > 0 && (legalEntity == null || !legalEntity.Value))
        {
            return query.Where(co => genderTypes.Any(y => y == ((int)co.Gender)));
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

    public int[] CreateAddressTypeList(bool? homeAddress, bool? companyAddress, bool? invoiceAddress)
    {
        var tmp = new List<int>();

        if (homeAddress != null && homeAddress.Value)
        {
            tmp.Add(0);
        }

        if (companyAddress != null && companyAddress.Value)
        {
            tmp.Add(1);
        }

        if (invoiceAddress != null && invoiceAddress.Value)
        {
            tmp.Add(2);
        }

        return tmp.ToArray();
    }

    public int[] CreateGenderList(bool? male, bool? female, bool? legalEntity, bool? intersexuality)
    {
        var tmp = new List<int>();

        if (male != null && male.Value)
        {
            tmp.Add((int)GenderEnum.Male);
        }

        if (female != null && female.Value)
        {
            tmp.Add((int)GenderEnum.Female);
        }

        if (legalEntity != null && legalEntity.Value)
        {
            tmp.Add((int)GenderEnum.LegalEntity);
        }

        if (intersexuality != null && intersexuality.Value)
        {
            tmp.Add((int)GenderEnum.Intersexuality);
        }

        return tmp.ToArray();
    }

    public IQueryable<Client> ApplyEntityTypeFilter(IQueryable<Client> query, bool employee, bool externEmp, bool customer)
    {
        if (employee && externEmp && customer)
        {
            return query;
        }

        if (!employee && !externEmp && !customer)
        {
            return query.Where(x => false);
        }

        var selectedTypes = new List<int>();

        if (employee)
        {
            selectedTypes.Add((int)EntityTypeEnum.Employee);
        }

        if (externEmp)
        {
            selectedTypes.Add((int)EntityTypeEnum.ExternEmp);
        }

        if (customer)
        {
            selectedTypes.Add((int)EntityTypeEnum.Employee);
        }

        return query.Where(x => selectedTypes.Contains((int)x.Type));
    }
}