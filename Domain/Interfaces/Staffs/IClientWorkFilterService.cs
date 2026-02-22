// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IClientWorkFilterService
{
    IQueryable<Client> FilterByMembershipYearMonth(IQueryable<Client> query, int year, int month);
}
