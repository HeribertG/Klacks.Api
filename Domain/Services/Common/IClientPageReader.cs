// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

/// <summary>
/// The materialised client page of the central client base query (membership, group, search and type filters) that
/// the Application layer reads. The composable base query itself stays on the Infrastructure-only
/// IClientBaseQueryService.
/// </summary>
public interface IClientPageReader
{
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
