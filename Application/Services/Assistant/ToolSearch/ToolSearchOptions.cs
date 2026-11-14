// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runtime switch of the in-turn tool search, bound from Assistant:ToolSearch:Enabled at startup. Off by default
/// until the live measurement decides; off means find_tools is never offered and the toolset stays as assembled.
/// </summary>
/// <param name="Enabled">Whether find_tools joins every toolset</param>

namespace Klacks.Api.Application.Services.Assistant.ToolSearch;

public sealed record ToolSearchOptions(bool Enabled)
{
    public const string EnabledConfigKey = "Assistant:ToolSearch:Enabled";

    public const bool DefaultEnabled = false;
}
