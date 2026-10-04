// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Associations;

/// <summary>
/// The two facts group visibility decides a client by (ClientGroupFilterService): whether the client carries any
/// group item at all (none = visible to every planner), and which groups it is an active, non-scenario member of.
/// </summary>
/// <param name="HasAnyGroupItem">True when the client has at least one non-deleted group item of any kind</param>
/// <param name="ActiveGroupIds">Distinct groups of the client's non-scenario (AnalyseToken null) group items</param>
public sealed record ClientVisibilityMembership(bool HasAnyGroupItem, IReadOnlyList<Guid> ActiveGroupIds);
