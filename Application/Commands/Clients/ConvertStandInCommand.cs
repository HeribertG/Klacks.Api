// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a stand-in into an employee or an external employee in place: same id, works, membership, group items and
/// qualifications.
/// </summary>
/// <param name="ClientId">The stand-in to convert</param>
/// <param name="Resource">Target type and the person data the target type requires</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Clients;

public record ConvertStandInCommand(Guid ClientId, ConvertStandInResource Resource) : IRequest<ClientResource>;
