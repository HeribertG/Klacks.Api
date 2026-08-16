// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <param name="GroupId">Any group id in the target group's subtree; resolved to its root.</param>
/// <param name="OrderedUserIds">The full desired call order, first stage first.</param>
public record ReorderEscalationRosterCommand(Guid GroupId, IReadOnlyList<string> OrderedUserIds) : IRequest<HttpResultResource>;
