// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs every language-pack vocabulary loader once at application startup, all of them against the same pack
/// directory (LanguagePlugins:Directory when configured, the shipped Plugins/Languages folder otherwise), so a
/// deployment that relocates the packs moves every reader at once. A pack file that cannot be read is logged as a
/// warning and skipped; the loaders for the navigation, mutation, grouping, completion-claim and assistant-text
/// vocabulary, and the conversation-signals loader with its collision table, are the ones started here.
/// </summary>
/// <param name="baseDirectory">Application base directory a relative pack directory is resolved against</param>
/// <param name="configuredPluginDirectory">Value of LanguagePlugins:Directory, null when not configured</param>
/// <param name="logger">Receives a warning per unreadable pack file and per pack without assistant texts, and the collision table</param>

using Klacks.Api.Application.Constants;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Klacksy;

public static class LanguagePluginStartupLoader
{
    private const string LoadFailedLogTemplate = "Language plugin file failed to load: {File}";
    private const string MissingAssistantTextsLogTemplate =
        "Language pack '{Code}' has no " + LanguagePluginConstants.AssistantTextsFileName
        + "; its assistant texts go out in English";

    public static void LoadAll(string baseDirectory, string? configuredPluginDirectory, ILogger logger)
    {
        void OnError(string file, Exception ex) => logger.LogWarning(ex, LoadFailedLogTemplate, file);

        NavigationIntentPluginLoader.Load(baseDirectory, OnError, configuredPluginDirectory);
        MutationIntentPluginLoader.Load(baseDirectory, OnError, configuredPluginDirectory);
        GroupingIntentPluginLoader.Load(baseDirectory, OnError, configuredPluginDirectory);
        ConversationSignalsPluginLoader.Load(
            baseDirectory, configuredPluginDirectory: configuredPluginDirectory, logger: logger);
        AssistantTextsPluginLoader.Load(
            baseDirectory,
            OnError,
            code => logger.LogWarning(MissingAssistantTextsLogTemplate, code),
            configuredPluginDirectory);
        CompletionClaimPluginLoader.Load(baseDirectory, OnError, configuredPluginDirectory);
    }
}
