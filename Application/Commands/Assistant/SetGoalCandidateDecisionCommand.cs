// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record SetGoalCandidateDecisionCommand : IRequest<bool>
{
    public Guid Id { get; init; }

    public string UserId { get; init; } = string.Empty;

    public string Decision { get; init; } = string.Empty;

    public IReadOnlyList<string> UserPermissions { get; init; } = Array.Empty<string>();
}
