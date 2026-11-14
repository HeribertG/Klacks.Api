// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names and limits of the in-turn tool search: the find_tools skill lets the model ask for skills its toolset is
/// missing, and the skills it finds join the toolset from the next tool-loop iteration on. The limits keep the grown
/// toolset small: at most MaxNeedsPerCall descriptions per call, HitsPerNeed skills per description,
/// MaxAddedToolsPerTurn added skills and MaxSearchesPerTurn searches per turn in total. The context budget reserves
/// room for MaxAddedToolsPerTurn extra tool definitions whenever find_tools is offered.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ToolSearchConstants
{
    public const string SkillName = "find_tools";

    public const string NeedsParameter = "needs";

    public const char NeedSeparator = ';';

    public const int MaxNeedsPerCall = 3;

    public const int HitsPerNeed = 3;

    public const int MaxAddedToolsPerTurn = 9;

    public const int MaxSearchesPerTurn = 2;
}
