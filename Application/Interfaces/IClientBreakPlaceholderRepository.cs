// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientBreakPlaceholderRepository
{
    Task<(List<Client> Clients, int TotalCount)> BreakList(BreakFilter filter, CancellationToken cancellationToken = default);
}
