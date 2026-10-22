// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;

namespace Klacks.Api.Application.Interfaces;

public interface IClientFilterRepository
{
    Task<PagedResult<Client>> GetFilteredClients(ClientFilter filter, PaginationParams pagination);

    /// <summary>
    /// The export rows of every client the filter keeps (the same pipeline as the client list, including the
    /// deleted-entries view), narrowed to onlyIds and without excludedIds, in IdNumber order whatever the filter sorts by.
    /// </summary>
    /// <param name="filter">The client list filter; its sort is replaced by the IdNumber order</param>
    /// <param name="onlyIds">When set, only these clients are exported; null = no restriction</param>
    /// <param name="excludedIds">When set, these clients are left out; null = nobody is left out</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<ClientExportRow>> GetExportRowsAsync(
        ClientFilter filter,
        IReadOnlyCollection<Guid>? onlyIds,
        IReadOnlyCollection<Guid>? excludedIds,
        CancellationToken cancellationToken = default);
}
