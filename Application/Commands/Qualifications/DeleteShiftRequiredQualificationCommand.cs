// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a shift-required-qualification row by id.
/// </summary>
/// <param name="Id">Id of the row to delete</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Qualifications;

public record DeleteShiftRequiredQualificationCommand(Guid Id) : IRequest;
