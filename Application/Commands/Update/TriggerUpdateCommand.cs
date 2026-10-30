// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Update;

/// <summary>
/// Requests the update the current channel manifest offers. The manifest alone names the version and the artifact.
/// </summary>
/// <param name="RequestedBy">User id of the administrator, or UpdateRequesters.Automatic for the automatic policy</param>
/// <param name="ExpectedTargetVersion">The version a confirmation showed; the command is rejected when the manifest offers another one. Null accepts whatever is offered</param>
public record TriggerUpdateCommand(string RequestedBy, string? ExpectedTargetVersion = null) : IRequest<UpdateTriggerResult>;
