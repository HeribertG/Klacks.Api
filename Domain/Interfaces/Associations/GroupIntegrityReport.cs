// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Report containing results of group integrity checks
/// </summary>
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public class GroupIntegrityReport
{
    public IEnumerable<Group> OrphanedGroups { get; set; } = Enumerable.Empty<Group>();
    public IEnumerable<Group> CircularReferences { get; set; } = Enumerable.Empty<Group>();
    public IEnumerable<Group> NestedSetInconsistencies { get; set; } = Enumerable.Empty<Group>();
    public IEnumerable<string> ValidationErrors { get; set; } = Enumerable.Empty<string>();
    public bool IsIntegrityValid => !OrphanedGroups.Any() && 
                                   !CircularReferences.Any() && 
                                   !NestedSetInconsistencies.Any() && 
                                   !ValidationErrors.Any();
}
