// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

/// <summary>
/// The sub-works the work changes of a container save are attached to, with the ids of all sub-works stored under the
/// container.
/// </summary>
/// <param name="SubWorks">Sub-work by id: the ones the request names, and the stored ones only a work change refers to</param>
/// <param name="StoredSubWorkIds">Ids of the not deleted sub-works stored under the container</param>
public sealed record WorkChangeParents(IReadOnlyDictionary<Guid, Work> SubWorks, IReadOnlyCollection<Guid> StoredSubWorkIds);
