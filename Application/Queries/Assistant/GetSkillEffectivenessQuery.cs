// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// W6.1: asks for the aggregated skill-effectiveness scorecard (eval trend, recipe funnel,
/// failure classes, top/flop skills, toolset provenance). Read-only; admin-only at the controller.
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetSkillEffectivenessQuery : IRequest<SkillEffectivenessResource>
{
    /// <summary>
    /// Length of the reporting window in days, counted back from now over create_time. Bounded by
    /// SkillEffectivenessDefaults; the controller rejects values outside the range rather than
    /// clamping them, so a caller never gets a different period than it asked for.
    /// </summary>
    public int Days { get; set; } = SkillEffectivenessDefaults.DefaultDays;
}
