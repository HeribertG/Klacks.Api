// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Services.Clients;

public interface IClientSortingService
{
    IQueryable<Client> ApplySorting(IQueryable<Client> query, string? orderBy, string? sortOrder);
}