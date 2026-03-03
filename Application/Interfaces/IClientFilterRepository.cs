// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;

namespace Klacks.Api.Application.Interfaces;

public interface IClientFilterRepository
{
    Task<PagedResult<Client>> GetFilteredClients(ClientFilter filter, PaginationParams pagination);
    Task<IQueryable<Client>> FilterClients(ClientFilter filter);
}
