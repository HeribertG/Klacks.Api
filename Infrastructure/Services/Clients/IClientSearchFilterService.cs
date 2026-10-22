// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Services.Clients;

/// <summary>
/// Applies the client search string (id numbers, a single id number or the text search) to a client query, for the
/// Infrastructure reads that compose on it.
/// </summary>
public interface IClientSearchFilterService
{
    IQueryable<Client> ApplySearchFilter(IQueryable<Client> query, string searchString, bool includeAddress = false);
}