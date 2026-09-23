// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Plugins;

public interface IFeaturePluginAssistantSetupHintService
{
    Task<string> AppendHintAsync(string pluginName, string message, string? userLanguage);
}
