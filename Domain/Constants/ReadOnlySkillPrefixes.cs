// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Name prefixes that identify read-only (non-mutating) skills. After a tool turn the LLM
/// loop reduces the available function set to these so a follow-up turn can only read, never
/// write. Centralized so the prefix contract stays in sync with skill naming conventions.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ReadOnlySkillPrefixes
{
    public const string Get = "get_";

    public const string List = "list_";

    public const string Search = "search_";
}
