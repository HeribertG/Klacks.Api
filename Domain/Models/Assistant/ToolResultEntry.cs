// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// One executed tool call as rendered into the tool-result block that is fed back to a model.
/// </summary>
/// <param name="Name">Skill name the model called; escaped by the formatter like the result body.</param>
/// <param name="Result">Raw result text; null renders the empty-result placeholder.</param>
/// <param name="ContainsExternalContent">True when the result carries relayed external content; the
/// formatter additionally treats every name listed in UntrustedSkillOutputs as external.</param>
public record ToolResultEntry(string Name, string? Result, bool ContainsExternalContent = false);
