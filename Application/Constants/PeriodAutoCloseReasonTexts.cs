// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Plain English sentences, per PeriodAutoCloseBlockedBy, that tell a user why Klacksy does or does not close a
/// group's periods on its own. They name the settings by the labels the settings pages show (Klacksy Scope of
/// Action, Klacksy Autonomy, the rule "Automatic period close", the level "Carry out"), never by internal names
/// such as enum members or rule kinds, because the model repeats what a skill result shows it; the model
/// translates them into the user's language. Read by get_period_close_schedule.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Constants;

public static class PeriodAutoCloseReasonTexts
{
    public const string Allowed =
        "Klacksy closes this group's periods on its own once the close date has been reached, provided the period then contains no errors.";

    public const string KillSwitch =
        "The master off switch for self-directed action in Klacksy Scope of Action is on, so Klacksy only reminds.";

    public const string RuleDisabled =
        "The rule \"Automatic period close\" is switched off in Klacksy Scope of Action, so Klacksy only reminds.";

    public const string RuleBelowCarryOut =
        "The rule \"Automatic period close\" in Klacksy Scope of Action is not set to \"Carry out\" (its default only reports), so Klacksy only reminds.";

    public const string GlobalLevel =
        "The global autonomy level in Klacksy Scope of Action is not \"Carry out, including multi-step\", so Klacksy only reminds.";

    public const string AdminLevelMissing =
        "At least one administrator has not yet chosen a level under Klacksy Autonomy, so Klacksy only reminds.";

    public const string NoAdmins =
        "There is no administrator whose consent could allow the automatic close, so Klacksy only reminds.";

    public const string AdminLevel =
        "Not every administrator has chosen \"Fully autonomous\" under Klacksy Autonomy, so Klacksy only reminds.";

    public const string NoDecidingAdmin =
        "No administrator could be determined in whose name the close would be recorded, so Klacksy only reminds.";

    /// <summary>The user-facing reason for one brake; every enum member has its own sentence.</summary>
    public static string For(PeriodAutoCloseBlockedBy blockedBy) => blockedBy switch
    {
        PeriodAutoCloseBlockedBy.None => Allowed,
        PeriodAutoCloseBlockedBy.KillSwitch => KillSwitch,
        PeriodAutoCloseBlockedBy.KindDisabled => RuleDisabled,
        PeriodAutoCloseBlockedBy.MaxAction => RuleBelowCarryOut,
        PeriodAutoCloseBlockedBy.GlobalLevel => GlobalLevel,
        PeriodAutoCloseBlockedBy.AdminLevelMissing => AdminLevelMissing,
        PeriodAutoCloseBlockedBy.NoAdmins => NoAdmins,
        PeriodAutoCloseBlockedBy.AdminLevel => AdminLevel,
        PeriodAutoCloseBlockedBy.NoDecidingAdmin => NoDecidingAdmin,
        _ => throw new ArgumentOutOfRangeException(nameof(blockedBy), blockedBy, "Unknown period auto-close brake.")
    };
}
