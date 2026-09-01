// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// W6.1: asks for the aggregated skill-effectiveness scorecard (eval trend, recipe funnel,
/// failure classes, top/flop skills, toolset provenance). Read-only; admin-only at the controller.
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetSkillEffectivenessQuery : IRequest<SkillEffectivenessResource>
{
}
