// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Role names of a conversation message as every supported provider expects them on the wire. Introduced
/// 2026-09-16 for the correction work; the older call sites still spell the strings out and are left
/// alone here on purpose - a rename sweep across the chat loop is its own change, not a side effect.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class LLMMessageRoles
{
    public const string User = "user";

    public const string Assistant = "assistant";
}
