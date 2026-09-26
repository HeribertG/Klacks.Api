// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Counts the close attempts of ONE PeriodAutoCloseService scan against PeriodAutoClose.MaxClosesPerTick. Lives
/// for a single RunAsync call only, so the cap is per scan and never carried over; several API instances each
/// apply their own cap (the service has no cross-instance protection, see PeriodAutoCloseService).
/// </summary>
/// <param name="maxAttempts">How many close attempts the scan may make.</param>

namespace Klacks.Api.Application.Services.Assistant.Triggers;

internal sealed class PeriodAutoCloseBudget
{
    private readonly int _maxAttempts;
    private int _attempts;

    public PeriodAutoCloseBudget(int maxAttempts)
    {
        _maxAttempts = maxAttempts;
    }

    /// <summary>Takes one attempt; false when the scan has used up its cap.</summary>
    public bool TryTake()
    {
        if (_attempts >= _maxAttempts)
        {
            return false;
        }

        _attempts++;
        return true;
    }
}
