// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What every tour of one build_weekly_tours run shares.
/// </summary>
/// <param name="SkillName">Skill that asked, carried on a refusal</param>
/// <param name="Location">Base address every tour starts and ends at</param>
/// <param name="Plan">The planned tours with the resolved time budget and validity</param>
/// <param name="TransportMode">Transport mode of the routes</param>
/// <param name="TimeRangeTolerance">Strictness of the TimeRange windows</param>
/// <param name="NamePrefix">Prefix of the tour names; the tour number follows</param>
/// <param name="Groups">Groups the containers are linked to</param>
/// <param name="DefaultMacroId">Default Shift-category macro of the containers</param>
/// <param name="UserName">Name of the user recorded as creator</param>
/// <param name="InstanceId">Instance id the container locks are acquired under</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WeeklyTourBuildSettings(
    string SkillName,
    string Location,
    WeeklyTourPlan Plan,
    ContainerTransportMode TransportMode,
    double TimeRangeTolerance,
    string NamePrefix,
    IReadOnlyList<Group> Groups,
    Guid? DefaultMacroId,
    string UserName,
    string InstanceId);
