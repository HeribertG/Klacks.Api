using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientFilterService : IClientFilterService
{
    public IQueryable<Client> ApplyGenderFilter(IQueryable<Client> query, int[] genderTypes)
    {
        if (genderTypes.Length > 0)
        {
            return query.Where(co => genderTypes.Any(y => y == ((int)co.Gender)));
        }

        return query;
    }

    public IQueryable<Client> ApplyClientTypeFilter(IQueryable<Client> query, int[] clientsTypes)
    {
        if (clientsTypes.Length > 0)
        {
            return query.Where(co => clientsTypes.Contains((int)co.Type));
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

    public IQueryable<Client> ApplyStateOrCountryFilter(IQueryable<Client> query, List<StateCountryFilter> stateTokens, List<string> countries)
    {
        if (stateTokens?.Any() == true || countries?.Any() == true)
        {
            var selectedTokens = stateTokens?.Where(x => x.Select).ToList() ?? new List<StateCountryFilter>();
            
            var selectedCountries = new List<string>();
            if (countries?.Any() == true)
            {
                selectedCountries = countries.Select(x => x.ToLower()).ToList();
            }

            System.Console.WriteLine($"StateFilter - StateTokens count: {stateTokens?.Count ?? 0}, Selected: {selectedTokens.Count}, Countries: {selectedCountries.Count}");
            if (selectedCountries.Any())
                System.Console.WriteLine($"StateFilter - Countries list: [{string.Join(", ", selectedCountries)}]");

            if (selectedTokens.Any())
            {
                System.Console.WriteLine("StateFilter - Applying state+country pairs filter");
                
                var validStateCountryKeys = new List<string>();
                foreach (var token in selectedTokens)
                {
                    if (selectedCountries.Any())
                    {
                        if (selectedCountries.Contains(token.Country.ToLower()))
                        {
                            validStateCountryKeys.Add((token.State + "|" + token.Country).ToLower());
                        }
                    }
                    else
                    {
                        validStateCountryKeys.Add((token.State + "|" + token.Country).ToLower());
                    }
                }
                
                System.Console.WriteLine($"StateFilter - Valid state+country combinations: [{string.Join(", ", validStateCountryKeys)}]");
                
                if (validStateCountryKeys.Any())
                {
                    return query.Where(co => co.Addresses.Any(ad => 
                        validStateCountryKeys.Contains((ad.State + "|" + ad.Country).ToLower())));
                }
                else
                {
                    return query.Where(co => false);
                }
            }
            else if (selectedCountries.Any())
            {
                System.Console.WriteLine("StateFilter - Applying country-only filter");
                return query.Where(co => co.Addresses.Any(ad => selectedCountries.Contains(ad.Country.ToLower())));
            }
        }

        System.Console.WriteLine("StateFilter - No filter applied");
        return query;
    }

    public int[] CreateAddressTypeList(bool? homeAddress, bool? companyAddress, bool? invoiceAddress)
    {
        var lst = new List<int>();

        if (homeAddress != null && homeAddress.Value)
        {
            lst.Add(0);
        }

        if (companyAddress != null && companyAddress.Value)
        {
            lst.Add(1);
        }

        if (invoiceAddress != null && invoiceAddress.Value)
        {
            lst.Add(2);
        }

        return lst.ToArray();
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

        if (intersexuality != null && intersexuality.Value)
        {
            tmp.Add((int)GenderEnum.Intersexuality);
        }

        if (legalEntity != null && legalEntity.Value)
        {
            tmp.Add((int)GenderEnum.LegalEntity);
        }

        return tmp.ToArray();
    }

    public int[] CreateClientTypeList(bool? employee, bool? customer, bool? externEmp)
    {
        var tmp = new List<int>();

        if (employee != null && employee.Value)
        {
            tmp.Add((int)EntityTypeEnum.Employee);
        }

        if (customer != null && customer.Value)
        {
            tmp.Add((int)EntityTypeEnum.Customer);
        }

        if (externEmp != null && externEmp.Value)
        {
            tmp.Add((int)EntityTypeEnum.ExternEmp);
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
            selectedTypes.Add((int)EntityTypeEnum.Customer);
        }

        return query.Where(x => selectedTypes.Contains((int)x.Type));
    }

    
}