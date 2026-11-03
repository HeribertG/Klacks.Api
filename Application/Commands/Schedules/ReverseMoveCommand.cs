// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

/// <param name="RequestId">The applied Move row to take back</param>
public record ReverseMoveCommand(Guid RequestId) : IRequest<ReassignWorkClientResponse?>;
