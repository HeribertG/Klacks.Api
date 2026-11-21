// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Update;

/// <summary>
/// Queues the PostgreSQL engine upgrade the current channel manifest offers for this installation. The manifest alone
/// names the target major and the signed engine package.
/// </summary>
/// <param name="RequestedBy">User id of the administrator who started it</param>
public record TriggerEngineUpgradeCommand(string RequestedBy) : IRequest<EngineUpgradeTriggerResult>;
