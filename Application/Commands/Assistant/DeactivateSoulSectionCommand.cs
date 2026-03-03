// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class DeactivateSoulSectionCommand : IRequest<Unit>
{
    public Guid AgentId { get; set; }
    public string SectionType { get; set; } = string.Empty;
}
