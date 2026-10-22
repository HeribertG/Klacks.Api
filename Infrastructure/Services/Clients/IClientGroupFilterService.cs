// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Infrastructure.Services.Clients;

/// <summary>
/// Narrows a client query to what the selected group and the caller's group visibility allow (see
/// ClientGroupFilterService), for the Infrastructure reads that compose on it; Application code only asks
/// ICallerGroupRestriction.
/// </summary>
public interface IClientGroupFilterService : ICallerGroupRestriction
{
    Task<IQueryable<Client>> FilterClientsByGroupId(
        Guid? selectedGroupId, IQueryable<Client> query, bool withoutGroup = false);
}
