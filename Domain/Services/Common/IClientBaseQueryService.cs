// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Central service for client base queries with membership, group, search and type filters.
/// Returns IQueryable so that subsequent operations (includes, count, paging) remain in the DB.
/// </summary>
/// <param name="filter">Common filter with time period, search, group and sorting</param>
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Domain.Services.Common;

public interface IClientBaseQueryService
{
    Task<IQueryable<Client>> BuildBaseQuery(ClientBaseFilter filter);
}
