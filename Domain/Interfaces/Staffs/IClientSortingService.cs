// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IClientSortingService
{
    IQueryable<Client> ApplySorting(IQueryable<Client> query, string? orderBy, string? sortOrder);
}