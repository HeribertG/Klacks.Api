using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Interfaces.Domains;

public interface IClientSortingService
{
    IQueryable<Client> ApplySorting(IQueryable<Client> query, string? orderBy, string? sortOrder);
}