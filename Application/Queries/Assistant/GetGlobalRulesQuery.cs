// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetGlobalRulesQuery : IRequest<object>
{
    public int HistoryLimit { get; set; } = AssistantLimitDefaults.HistoryLimit;
    public bool IncludeHistory { get; set; }
}
