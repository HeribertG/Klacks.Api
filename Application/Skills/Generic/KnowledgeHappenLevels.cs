// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Explanation depth levels for knowledge happens and the markdown markers that delimit them.
/// A happen opts into levels by placing marker comments (e.g. "&lt;!-- level:short --&gt;")
/// before each level section; happens without markers always return their full content.
/// </summary>
namespace Klacks.Api.Application.Skills.Generic;

public static class KnowledgeHappenLevels
{
    public const string Short = "short";
    public const string Elements = "elements";
    public const string Effects = "effects";

    public const string ParameterName = "level";
    public const string MarkerPrefix = "<!-- level:";
    public const string MarkerSuffix = "-->";

    public static readonly IReadOnlyList<string> All = [Short, Elements, Effects];
}
