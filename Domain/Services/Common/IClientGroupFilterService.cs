// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

public interface IClientGroupFilterService
{
    Task<IQueryable<Client>> FilterClientsByGroupId(
        Guid? selectedGroupId, IQueryable<Client> query, bool withoutGroup = false);
}