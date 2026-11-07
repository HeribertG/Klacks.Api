// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates several stand-ins in one save: all of them or none.
/// </summary>
/// <param name="Resource">Count, name pattern, group, membership period and qualifications of the new stand-ins</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Clients;

public record CreateStandInsCommand(CreateStandInsResource Resource) : IRequest<List<CreatedStandInResource>>;
