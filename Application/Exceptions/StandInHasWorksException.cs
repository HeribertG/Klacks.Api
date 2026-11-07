// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A stand-in was not deleted because services of the real plan still lie on it. They would stay planned on a client
/// nobody sees, so they must first go to a person, or the stand-in is converted.
/// </summary>
/// <param name="standInName">Display name of the stand-in</param>
/// <param name="workCount">Number of real-plan works on the stand-in, container children included</param>
public sealed class StandInHasWorksException : ConflictException
{
    public const string ErrorCode = "STAND_IN_HAS_WORKS";
    public const string WorkCountKey = "workCount";
    public const string StandInNameKey = "standInName";

    public StandInHasWorksException(string standInName, int workCount)
        : base(BuildMessage(standInName, workCount), ErrorCode, BuildDetails(standInName, workCount))
    {
        WorkCount = workCount;
    }

    public int WorkCount { get; }

    private static string BuildMessage(string standInName, int workCount) =>
        $"The stand-in {standInName} cannot be deleted: {workCount} service(s) are still assigned to it. " +
        "Assign them to a person or convert the stand-in, then delete it.";

    private static IReadOnlyDictionary<string, object?> BuildDetails(string standInName, int workCount) =>
        new Dictionary<string, object?>
        {
            [StandInNameKey] = standInName,
            [WorkCountKey] = workCount
        };
}
