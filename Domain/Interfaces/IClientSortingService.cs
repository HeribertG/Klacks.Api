using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces;

public interface IClientSortingService
{
    IQueryable<Client> ApplySorting(IQueryable<Client> query, string? orderBy, string? sortOrder);
}