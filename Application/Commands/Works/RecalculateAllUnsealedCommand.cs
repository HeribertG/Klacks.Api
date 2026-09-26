// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Queues a thorough recalculation of every unsealed real-mode work, work change and break, whatever its date: the
/// macros run again for each of them and the period hours of all clients are recalculated. Sealed entries stay untouched.
/// ThoroughRecalculationBackgroundService finds the covered span itself and works month by month.
/// </summary>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record RecalculateAllUnsealedCommand : IRequest<bool>;
