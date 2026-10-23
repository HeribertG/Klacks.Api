// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scale and lookup sizes of agent memories: the importance range every writer clamps to (1-10, default 5), and
/// how many nearest neighbours the duplicate checks of the auto-extraction and the turn reflection look at.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentMemoryDefaults
{
    public const int MinImportance = 1;

    public const int MaxImportance = 10;

    public const int DefaultImportance = 5;

    public const int DuplicateCheckNeighbourLimit = 3;
}
