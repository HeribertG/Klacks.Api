// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fixed parameters of the autonomous period close.
/// WindowDays bounds how late an automatic close may still run: counted from the first day a close is
/// allowed (the day after the period end, or the close date when the lag is longer). The period_close_due
/// reminder announces a close during the three days before its close date; a period whose close date lies
/// further back than this window was never announced as an automatic close - typically the owner switched
/// full autonomy on long after the period ended - and is left to a person instead of being sealed on the
/// next scan without warning.
/// ReasonFormat is the seal reason written into the day locks and the audit trail; {0} is the close lag.
/// MaxClosesPerTick caps how many closes ONE scan may attempt (every attempt that reaches the close handler
/// counts, whatever its outcome). A seal is not cleanly reversible and fires the payroll export, so a faulty
/// configuration must not seal every group in one go: after at most this many seals the planners get the
/// closed messages and an administrator can pull the kill switch before the next hourly scan. The rest is
/// reported as TickLimitReached and picked up by the next scan; at 24 scans a day this still covers
/// 24 x 3 x (WindowDays + 1) groups inside the close window. The governance rule's DailyActionBudget and
/// WindowActionLimit are deliberately not used: they are counted per group from condition-claim events, which
/// this path never writes, and a group is closed at most once per period anyway, so they could never bind.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PeriodAutoClose
{
    public const int WindowDays = 3;

    public const int MaxClosesPerTick = 3;

    public const string ReasonFormat = "Automatic close by Klacksy (fully autonomous, close lag {0} day(s))";
}
