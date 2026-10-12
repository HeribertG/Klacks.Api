// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How the chat loop treats a model call to a skill that is not part of the turn's toolset.
/// </summary>
public enum ToolsetMembershipMode
{
    /// <summary>The call is only flagged and logged; it still runs.</summary>
    Observe = 0,

    /// <summary>The call is rejected with an instructive result and does not run.</summary>
    Enforce = 1
}
