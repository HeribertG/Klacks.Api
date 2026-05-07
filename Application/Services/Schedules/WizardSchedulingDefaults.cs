// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Default scheduling-rule values applied to wizard contexts when neither the agent's
/// effective contract nor the global Klacks settings supply a positive value. Centralises
/// what was previously duplicated across <see cref="WizardContextBuilder"/> and
/// <see cref="WizardAgentSnapshotBuilder"/>.
/// </summary>
public static class WizardSchedulingDefaults
{
    public const int MaxConsecutiveDays = 6;

    public const double MinRestHours = 11.0;

    public const double MaxWeeklyHours = 50.0;

    public const double MaxDailyHours = 10.0;

    public const double DefaultMotivation = 0.5;
}
