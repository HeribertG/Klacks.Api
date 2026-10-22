// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Central service for client base queries with membership, group, search and type filters.
/// BuildBaseQuery returns IQueryable so that the Infrastructure callers keep includes, count and paging in the DB;
/// GetPageWithGroupItemsAsync is the materialised page the Application layer reads.
/// </summary>
/// <param name="filter">Common filter with time period, search, group and sorting</param>
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Domain.Services.Common;

public interface IClientBaseQueryService
{
    Task<IQueryable<Client>> BuildBaseQuery(ClientBaseFilter filter);

    /// <summary>
    /// One page of the base query with each client's group items (the non-deleted ones, scenario rows included), plus
    /// the number of clients the base query keeps. The page is cut after the base sort; a start row past the end gives
    /// an empty page with the full total. The clients are loaded the way the base query loads them (tracked).
    /// </summary>
    /// <param name="filter">Common filter with time period, search, group and sorting</param>
    /// <param name="startRow">Number of sorted clients to skip</param>
    /// <param name="rowCount">Maximum number of clients on the page</param>
    /// <param name="cancellationToken">Cancels the count and the page read</param>
    Task<(IReadOnlyList<Client> Clients, int TotalCount)> GetPageWithGroupItemsAsync(
        ClientBaseFilter filter, int startRow, int rowCount, CancellationToken cancellationToken = default);
}
