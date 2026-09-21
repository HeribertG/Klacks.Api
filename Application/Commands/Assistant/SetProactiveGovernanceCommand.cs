// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Patch request for the proactive governance rules. Every rule field is optional and only the
/// supplied ones are written, so a caller may change a single budget without restating the row.
/// TriggerKind null means "only the kill switch is being set"; KillSwitch null means "leave the global
/// switch alone".
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record SetProactiveGovernanceCommand(
    string? TriggerKind,
    Guid? GroupId,
    ProactiveMaxAction? MaxAction,
    bool? Enabled,
    int? DailyActionBudget,
    int? WindowActionLimit,
    int? WindowMinutes,
    bool? KillSwitch,
    AutonomyLevel? AutonomyLevel) : IRequest<ProactiveGovernanceDto>;
