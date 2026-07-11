// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A single group member absent on a given day, as listed in a group absence overlap analysis.
/// </summary>
/// <param name="ClientId">Id of the absent client.</param>
/// <param name="ClientName">Display name (first name and last name) of the absent client.</param>
/// <param name="AbsenceType">Label of the absence type (e.g. vacation, sick leave).</param>

namespace Klacks.Api.Application.DTOs.Groups;

public record GroupAbsenceOverlapMember(
    Guid ClientId,
    string ClientName,
    string AbsenceType);
