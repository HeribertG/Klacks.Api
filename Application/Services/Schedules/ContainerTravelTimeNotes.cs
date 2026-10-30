// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The sentence the container-building skills append when the routing service returned no travel times and the autofill fell
/// back to straight-line estimates, so the model tells the user plainly that the travel times are only estimates.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerTravelTimeNotes
{
    private const string TravelTimesEstimatedNote =
        " The travel times are straight-line estimates: the routing service returned no travel times for the {0} " +
        "candidate service(s) — it could not be reached or the request exceeded its size limit (too many locations). " +
        "Tell the user explicitly that the travel times are only estimates.";

    private const string NarrowWithGroupNote =
        " With groupName the candidates can be narrowed to one group, so the routing service can compute real travel times.";

    /// <param name="isEstimated">True when the autofill used straight-line travel time estimates</param>
    /// <param name="candidateCount">Number of candidate services the routing request covered</param>
    /// <param name="groupFilterApplied">True when the candidates were already narrowed to a group</param>
    public static string Estimation(bool isEstimated, int candidateCount, bool groupFilterApplied)
    {
        if (!isEstimated)
        {
            return string.Empty;
        }

        var note = string.Format(CultureInfo.InvariantCulture, TravelTimesEstimatedNote, candidateCount);
        return groupFilterApplied ? note : note + NarrowWithGroupNote;
    }
}
