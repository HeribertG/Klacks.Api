// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything UnattendedSkillPolicy needs to judge one background skill run.
/// </summary>
/// <param name="SkillName">Name of the skill the background path wants to run.</param>
/// <param name="OwnerPermissions">Expanded permissions of the owner the run acts under; empty means no check is possible.</param>
/// <param name="AutonomyLevel">Autonomy level of that owner, read fresh at run time.</param>
/// <param name="ExecutionKind">Which background path is asking; only ScheduledTask honours the opt-in below.</param>
/// <param name="AllowIrreversibleUnattended">Per-task opt-in that lets an irreversible skill run unattended; ignored outside ScheduledTask.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record UnattendedSkillRequest(
    string SkillName,
    IReadOnlyList<string> OwnerPermissions,
    AutonomyLevel AutonomyLevel,
    UnattendedExecutionKind ExecutionKind,
    bool AllowIrreversibleUnattended);
