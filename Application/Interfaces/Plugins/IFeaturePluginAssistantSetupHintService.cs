// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Plugins;

public interface IFeaturePluginAssistantSetupHintService
{
    Task<string> AppendHintAsync(string pluginName, string message, string? userLanguage);
}
