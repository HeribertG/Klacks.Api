// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetGlobalRulesQuery : IRequest<object>
{
    public int HistoryLimit { get; set; } = 50;
    public bool IncludeHistory { get; set; }
}
