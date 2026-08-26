// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Argument names of the create_container_template skill, as the skill itself reads them. The
/// remediation binder builds its dictionary from these, and the dispatcher's bindability pre-flight
/// checks <see cref="Required"/> before it claims a condition - an unbindable row must cost neither an
/// attempt nor a slot of the daily action budget.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class CreateContainerTemplateParameters
{
    public const string SkillName = "create_container_template";

    public const string ContainerId = "containerId";
    public const string Weekday = "weekday";
    public const string FromTime = "fromTime";
    public const string UntilTime = "untilTime";
    public const string IsHoliday = "isHoliday";
    public const string IsWeekdayAndHoliday = "isWeekdayAndHoliday";

    /// <summary>Time format the skill parses with TimeOnly.TryParse.</summary>
    public const string TimeFormat = "HH\\:mm";

    public static readonly IReadOnlyList<string> Required = [ContainerId, Weekday, FromTime, UntilTime];
}
