using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Domain.Interfaces;

public interface IClientFilterService
{
    IQueryable<Client> ApplyGenderFilter(IQueryable<Client> query, int[] genderTypes);

    IQueryable<Client> ApplyClientTypeFilter(IQueryable<Client> query, int[] clientsTypes);

    IQueryable<Client> ApplyAddressTypeFilter(IQueryable<Client> query, int[] addressTypes);

    IQueryable<Client> ApplyAnnotationFilter(IQueryable<Client> query, bool? hasAnnotation);

    IQueryable<Client> ApplyStateOrCountryFilter(IQueryable<Client> query, List<StateCountryFilter> stateTokens, List<string> countries);

    int[] CreateAddressTypeList(bool? homeAddress, bool? companyAddress, bool? invoiceAddress);

    int[] CreateGenderList(bool? male, bool? female, bool? legalEntity, bool? intersexuality);

    int[] CreateClientTypeList(bool? employee, bool? customer, bool? externEmp);

    IQueryable<Client> ApplyEntityTypeFilter(IQueryable<Client> query, bool employee, bool externEmp, bool customer);
}