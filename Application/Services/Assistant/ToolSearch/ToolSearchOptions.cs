// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runtime switch of the in-turn tool search, bound from Assistant:ToolSearch:Enabled at startup. Off by default again
/// since 2026-11-16: a recorded multi-step take (book a container week, re-optimise routes) failed in 5 of 5 attempts
/// with it on and passed on the first attempt with it off; off means find_tools is never offered and the toolset stays
/// as assembled.
/// </summary>
/// <param name="Enabled">Whether find_tools joins every toolset</param>

namespace Klacks.Api.Application.Services.Assistant.ToolSearch;

public sealed record ToolSearchOptions(bool Enabled)
{
    public const string EnabledConfigKey = "Assistant:ToolSearch:Enabled";

    public const bool DefaultEnabled = false;
}
