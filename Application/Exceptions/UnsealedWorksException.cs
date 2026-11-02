// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.PeriodClosing;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A period close was refused because services of the period are not sealed yet. Unlike the unresolved errors of
/// PeriodValidationConflictException this cannot be acknowledged away: a close seals the period as a whole, and a
/// service that was never confirmed has no proof that it took place.
/// </summary>
/// <param name="unsealedCount">Number of parent works below Confirmed in the period, whether or not the caller may see them</param>
/// <param name="listed">The first of them, restricted to the rows the caller may see; carried as a structured field, never written into the message, which is logged</param>
public sealed class UnsealedWorksException : ConflictException
{
    public const string ErrorCode = "PERIOD_UNSEALED_WORKS";
    public const string UnsealedCountKey = "unsealedWorkCount";
    public const string WorksKey = "works";

    public UnsealedWorksException(int unsealedCount, IReadOnlyList<UnsealedWorkDto> listed)
        : base(BuildMessage(unsealedCount), ErrorCode, BuildDetails(unsealedCount, listed))
    {
        UnsealedCount = unsealedCount;
        Listed = listed;
    }

    public int UnsealedCount { get; }

    public IReadOnlyList<UnsealedWorkDto> Listed { get; }

    private static string BuildMessage(int unsealedCount) =>
        $"The period cannot be closed: {unsealedCount} service(s) are not confirmed yet. " +
        "Confirm each service after it has ended, then close the period.";

    private static IReadOnlyDictionary<string, object?> BuildDetails(int unsealedCount, IReadOnlyList<UnsealedWorkDto> listed) =>
        new Dictionary<string, object?>
        {
            [UnsealedCountKey] = unsealedCount,
            [WorksKey] = listed
        };
}
