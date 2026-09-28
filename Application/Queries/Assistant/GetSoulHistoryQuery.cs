// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetSoulHistoryQuery : IRequest<object>
{
    public Guid AgentId { get; set; }
    public int Limit { get; set; } = 50;
}
