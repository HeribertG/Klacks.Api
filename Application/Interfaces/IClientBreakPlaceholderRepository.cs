// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientBreakPlaceholderRepository
{
    Task<(List<Client> Clients, int TotalCount)> BreakList(BreakFilter filter);
}
