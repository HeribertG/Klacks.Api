// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

public interface IClientGroupFilterService
{
    Task<IQueryable<Client>> FilterClientsByGroupId(Guid? selectedGroupId, IQueryable<Client> query);
}