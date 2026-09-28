// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetAgentMemoriesQuery : IRequest<object>
{
    public Guid AgentId { get; set; }
    public string? Search { get; set; }
    public string? Category { get; set; }
}
