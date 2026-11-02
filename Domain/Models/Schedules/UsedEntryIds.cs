// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Which of the asked ids are taken by a row already, deleted rows included, since a deleted row keeps its key.
/// </summary>
/// <param name="Works">Ids taken in the work table</param>
/// <param name="Breaks">Ids taken in the break table</param>
/// <param name="WorkChanges">Ids taken in the work change table</param>
public sealed record UsedEntryIds(IReadOnlySet<Guid> Works, IReadOnlySet<Guid> Breaks, IReadOnlySet<Guid> WorkChanges);
