// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

public interface IClientSearchFilterService
{
    IQueryable<Client> ApplySearchFilter(IQueryable<Client> query, string searchString, bool includeAddress = false);
}