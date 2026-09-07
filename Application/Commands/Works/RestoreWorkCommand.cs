// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Undoes the soft-delete of a single Work and returns it in the same shape the delete answered with.
/// </summary>
/// <param name="Id">The soft-deleted Work to bring back</param>

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Works;

public record RestoreWorkCommand(Guid Id) : IRequest<WorkResource?>;
