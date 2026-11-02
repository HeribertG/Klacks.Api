// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a cover run may do with the works it cloned, by source work id.
/// </summary>
/// <param name="Movable">Works this report may move as a whole</param>
/// <param name="Moved">Works an earlier cover moved; no swap takes them from their person</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record CoverSourceWorks(IReadOnlySet<Guid> Movable, IReadOnlySet<Guid> Moved);
