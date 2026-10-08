// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkChangeRepository : IBaseRepository<WorkChange>
{
    /// <summary>
    /// Loads a WorkChange (untracked) with its parent Work regardless of its scope: unlike Get, scenario rows
    /// (AnalyseToken set) are returned too. For write paths that must reach scenario changes.
    /// </summary>
    /// <param name="id">Id of the WorkChange</param>
    Task<WorkChange?> GetWithWorkInAnyScope(Guid id);
}
