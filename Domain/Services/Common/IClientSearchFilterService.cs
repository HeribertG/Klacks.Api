// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

public interface IClientSearchFilterService
{
    IQueryable<Client> ApplySearchFilter(IQueryable<Client> query, string searchString, bool includeAddress = false);
}