// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Default scheduling-rule values applied to wizard contexts when neither the agent's
/// effective contract nor the global Klacks settings supply a positive cap. Rest minimums have no
/// wizard default; they are passed through as configured. Centralises
/// what was previously duplicated across <see cref="WizardContextBuilder"/> and
/// <see cref="WizardAgentSnapshotBuilder"/>.
/// </summary>
public static class WizardSchedulingDefaults
{
    public const int MaxConsecutiveDays = 6;

    public const double MaxWeeklyHours = 50.0;

    public const double MaxDailyHours = 10.0;

    public const double DefaultMotivation = 0.5;

    public const double MaxOptimalGap = 2.0;

    public const int MaxWorkDays = 5;
}
