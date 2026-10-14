// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Tells whether a skill shows a server-computed preview before it runs, that is, whether a registered
/// ISkillConfirmationPreviewProvider supports it. Such a skill only runs as the confirmed replay of that preview in the
/// chat, so background paths (an agent plan, a goal plan) must neither offer it nor try to run it.
/// </summary>
/// <param name="skillName">Name of the skill</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConfirmationPreviewCatalog
{
    bool HasPreview(string skillName);
}
