// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// The toolset consequences of the draft loops open in a conversation: the loop skills that must stay in the toolset
/// (the answers of a loop carry no keyword retrieval could rank on) and the skills that must stay out while the loop
/// runs, because they would change the same objects past the loop's own confirmed preview.
/// </summary>
/// <param name="GuaranteedSkillNames">Loop skills kept in the toolset</param>
/// <param name="ExcludedSkillNames">Skills left out of the toolset while the loop is open</param>
public sealed record DraftLoopToolset(IReadOnlyList<string> GuaranteedSkillNames, IReadOnlySet<string> ExcludedSkillNames)
{
    public static DraftLoopToolset None { get; } =
        new([], new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}
