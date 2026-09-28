// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Qualifications;

/// <summary>
/// Soft-deletes a qualification master entry by id.
/// </summary>
/// <param name="Id">Id of the qualification to delete</param>
public record DeleteQualificationCommand(Guid Id) : IRequest;
