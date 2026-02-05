using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IClientSearchService
{
    IQueryable<Client> ApplySearchFilter(IQueryable<Client> query, string searchString, bool includeAddress);
    IQueryable<Client> ApplyExactSearch(IQueryable<Client> query, string[] keywords, bool includeAddress);
    IQueryable<Client> ApplyStandardSearch(IQueryable<Client> query, string[] keywords, bool includeAddress);
    IQueryable<Client> ApplyFirstSymbolSearch(IQueryable<Client> query, string keyword);
    IQueryable<Client> ApplyPhoneOrZipSearch(IQueryable<Client> query, string number);
    IQueryable<Client> ApplyIdNumberSearch(IQueryable<Client> query, int idNumber);
    string[] ParseSearchString(string searchString);
    bool IsNumericSearch(string searchString);
}