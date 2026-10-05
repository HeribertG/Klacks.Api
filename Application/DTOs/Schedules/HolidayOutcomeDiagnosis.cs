// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record HolidayOutcomeDiagnosis
{
    public required DateOnly Date { get; init; }

    public required bool HasActiveContract { get; init; }

    public string? ContractName { get; init; }

    public required ResolvedHolidayCalendarSource Calendar { get; init; }

    public MultiLanguage? HolidayName { get; init; }

    public required HolidayStatus Status { get; init; }

    public required bool DowngradedByReminderOnly { get; init; }

    public required bool EarnsHolidayTimeSurcharge { get; init; }

    public required decimal HolidayRate { get; init; }

    public required string SurchargeReason { get; init; }

    public required bool HolidayWorkWarningRaised { get; init; }

    public required string WarningReason { get; init; }

    public string? ExemptionDescription { get; init; }

    public string? ExemptionSchedulingRuleName { get; init; }

    public required RuleEnforcementMode EnforcementMode { get; init; }

    public required IReadOnlyList<HolidayOutcomeWork> WorksOnDate { get; init; }
}
