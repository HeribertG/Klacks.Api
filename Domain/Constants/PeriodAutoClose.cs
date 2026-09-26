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
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PeriodAutoClose
{
    public const int WindowDays = 3;

    public const string ReasonFormat = "Automatic close by Klacksy (fully autonomous, close lag {0} day(s))";
}
