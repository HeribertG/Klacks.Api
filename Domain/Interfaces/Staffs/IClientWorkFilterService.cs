// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IClientWorkFilterService
{
    IQueryable<Client> FilterByMembershipYearMonth(IQueryable<Client> query, int year, int month);
}
