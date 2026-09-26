// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What a period unseal did with the Closed entries it reopened, split by the lock level each entry had
/// right before the period seal. Entries without a recorded level (sealed before the pre-seal state was
/// stored) reopen to None, exactly as every unseal did before.
/// </summary>
/// <param name="RestoredConfirmed">Entries set back to Confirmed</param>
/// <param name="RestoredApproved">Entries set back to Approved</param>
/// <param name="RestoredNone">Entries that were open before the seal and are open again</param>
/// <param name="WithoutRecordedLevel">Entries without a recorded pre-seal level, reopened to None</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record PeriodUnsealCounts(
    int RestoredConfirmed,
    int RestoredApproved,
    int RestoredNone,
    int WithoutRecordedLevel)
{
    public static PeriodUnsealCounts Empty { get; } = new(0, 0, 0, 0);

    public int Total => RestoredConfirmed + RestoredApproved + RestoredNone + WithoutRecordedLevel;

    /// <summary>
    /// Builds the counts from the pre-seal levels of the entries about to be reopened. A recorded level other
    /// than Confirmed or Approved cannot come from a period seal (it only raises entries below Closed) and is
    /// counted as None, the level the unseal falls back to for it.
    /// </summary>
    /// <param name="countsByPreSealLevel">Number of entries per recorded pre-seal level, null = not recorded</param>
    public static PeriodUnsealCounts FromPreSealLevels(IEnumerable<(WorkLockLevel? Level, int Count)> countsByPreSealLevel)
    {
        var confirmed = 0;
        var approved = 0;
        var none = 0;
        var withoutRecord = 0;

        foreach (var (level, count) in countsByPreSealLevel)
        {
            switch (level)
            {
                case null:
                    withoutRecord += count;
                    break;
                case WorkLockLevel.Confirmed:
                    confirmed += count;
                    break;
                case WorkLockLevel.Approved:
                    approved += count;
                    break;
                default:
                    none += count;
                    break;
            }
        }

        return new PeriodUnsealCounts(confirmed, approved, none, withoutRecord);
    }

    public static PeriodUnsealCounts operator +(PeriodUnsealCounts left, PeriodUnsealCounts right) =>
        new(
            left.RestoredConfirmed + right.RestoredConfirmed,
            left.RestoredApproved + right.RestoredApproved,
            left.RestoredNone + right.RestoredNone,
            left.WithoutRecordedLevel + right.WithoutRecordedLevel);
}
