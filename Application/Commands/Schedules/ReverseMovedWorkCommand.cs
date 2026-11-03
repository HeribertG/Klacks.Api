// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

/// <param name="WorkId">The moved work to give back to the person it came from</param>
public record ReverseMovedWorkCommand(Guid WorkId) : IRequest<ReassignWorkClientResponse?>;
