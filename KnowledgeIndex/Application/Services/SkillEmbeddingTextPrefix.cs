// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The opening of every skill's embedding text: name, ". ", description, newline. Part of the hashed text, so
/// changing it re-embeds the whole index. The synchronizer builds the text with it and the learning gate
/// checks the stored text against it, so the two can never disagree about what "indexed" means.
/// </summary>
/// <param name="skillName">Registry name of the skill, the index source id</param>
/// <param name="description">The description the text is built from</param>
namespace Klacks.Api.KnowledgeIndex.Application.Services;

public static class SkillEmbeddingTextPrefix
{
    private const string NameSeparator = ". ";
    private const char SectionSeparator = '\n';

    public static string Build(string skillName, string description) =>
        skillName + NameSeparator + description + SectionSeparator;
}
