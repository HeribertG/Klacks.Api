using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IClientChangeTrackingService
{
    Task<(DateTime lastChangeDate, List<Guid> clientIds, List<string> authors)> GetLastChangeMetadataAsync();

    Task<(IQueryable<Client> clients, List<string> authors, DateTime lastChangeDate)> GetLastChangedClientsAsync(IQueryable<Client> baseQuery, FilterResource filter);

    Task<LastChangeMetaDataResource> GetLastChangeMetadataResourceAsync();
}