// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetAgentSkillsQuery : IRequest<object>
{
    public Guid AgentId { get; set; }
}
