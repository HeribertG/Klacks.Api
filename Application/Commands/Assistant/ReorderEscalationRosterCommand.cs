// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <param name="OrderedUserIds">The full desired wake-up order, first user first.</param>
public record ReorderEscalationRosterCommand(IReadOnlyList<string> OrderedUserIds) : IRequest<HttpResultResource>;
