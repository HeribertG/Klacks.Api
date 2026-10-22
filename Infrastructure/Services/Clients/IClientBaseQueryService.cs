// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Infrastructure.Services.Clients;

/// <summary>
/// Central client base query with membership, group, search and type filters. BuildBaseQuery returns IQueryable so
/// that the Infrastructure callers keep includes, count and paging in the database; the Application layer reads the
/// materialised page through IClientPageReader.
/// </summary>
public interface IClientBaseQueryService : IClientPageReader
{
    /// <param name="filter">Common filter with time period, search, group and sorting</param>
    Task<IQueryable<Client>> BuildBaseQuery(ClientBaseFilter filter);
}
