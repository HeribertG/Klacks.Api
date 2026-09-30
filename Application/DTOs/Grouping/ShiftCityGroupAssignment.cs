// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// One planned move of a shift into a city group (a leaf of the group tree), derived from its customer's address.
/// </summary>
/// <param name="ShiftId">Id of the shift that would be moved.</param>
/// <param name="ShiftName">Name of the shift, for the preview text.</param>
/// <param name="Status">Lifecycle status of the shift (sealed order, plannable shift or split shift).</param>
/// <param name="CustomerName">Display name of the customer whose address decided the placement.</param>
/// <param name="GroupId">Id of the city group the shift would be linked to.</param>
/// <param name="GroupName">Name of that city group.</param>
/// <param name="MatchReason">How the city group was found, plus the address used.</param>
/// <param name="DistanceKm">Great-circle distance to the group location; only set for a nearest match.</param>
/// <param name="ReplacedGroupItemIds">Existing group links of the shift that the new link replaces.</param>
/// <param name="ReplacedGroupNames">Names of the groups behind the replaced links, for the preview text.</param>
/// <param name="ValidFrom">Start of the new link, carried over from the replaced links.</param>
/// <param name="ValidUntil">End of the new link, carried over from the replaced links.</param>
public sealed record ShiftCityGroupAssignment(
    Guid ShiftId,
    string ShiftName,
    ShiftStatus Status,
    string CustomerName,
    Guid GroupId,
    string GroupName,
    string MatchReason,
    double? DistanceKm,
    IReadOnlyList<Guid> ReplacedGroupItemIds,
    IReadOnlyList<string> ReplacedGroupNames,
    DateTime? ValidFrom,
    DateTime? ValidUntil);
