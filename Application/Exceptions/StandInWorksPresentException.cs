// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A period close was refused because services of the period still lie on stand-ins. Like UnsealedWorksException this
/// cannot be acknowledged away: a stand-in is no person, so its services cannot be sealed for payroll or customer billing.
/// </summary>
/// <param name="standInWorkCount">Number of top-level works of the period on stand-ins, whether or not the caller may see them</param>
public sealed class StandInWorksPresentException : ConflictException
{
    public const string ErrorCode = "PERIOD_STAND_IN_WORKS";
    public const string StandInWorkCountKey = "standInWorkCount";

    public StandInWorksPresentException(int standInWorkCount)
        : base(BuildMessage(standInWorkCount), ErrorCode, BuildDetails(standInWorkCount))
    {
        StandInWorkCount = standInWorkCount;
    }

    public int StandInWorkCount { get; }

    private static string BuildMessage(int standInWorkCount) =>
        $"The period cannot be closed: {standInWorkCount} service(s) are assigned to stand-ins. " +
        "Assign them to a person or convert the stand-ins, then close the period.";

    private static IReadOnlyDictionary<string, object?> BuildDetails(int standInWorkCount) =>
        new Dictionary<string, object?>
        {
            [StandInWorkCountKey] = standInWorkCount
        };
}
