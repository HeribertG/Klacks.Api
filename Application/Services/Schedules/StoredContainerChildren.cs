// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// The children of a container work as they are stored, before a save changes them.
/// </summary>
/// <param name="Works">The not deleted sub-works</param>
/// <param name="Breaks">The not deleted sub-breaks</param>
/// <param name="WorkChanges">The work changes of the sub-works</param>
public sealed record StoredContainerChildren(
    IReadOnlyList<Work> Works,
    IReadOnlyList<Break> Breaks,
    IReadOnlyList<WorkChange> WorkChanges);
