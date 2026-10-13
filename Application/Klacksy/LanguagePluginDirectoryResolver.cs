// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the directory the language packs are read from, the one way every reader must agree on: the configured
/// LanguagePlugins:Directory value when set (absolute as is, relative to the application base directory), the
/// shipped Plugins/Languages folder otherwise. Every runtime reader of the packs goes through it: LanguagePluginService
/// and LanguageConfigController (install and uninstall), the vocabulary loaders started by LanguagePluginStartupLoader
/// (navigation, mutation, grouping, conversation signals, assistant texts, completion claims), UtteranceNormalizer
/// (wake words) and PhoneticConfigProvider (phonetics), so a deployment that relocates the packs moves all of them.
/// TurnIntentFeatureSourceFingerprint is not a runtime reader: it hashes the source tree for a guard test.
/// </summary>
/// <param name="baseDirectory">Application base directory a relative path is resolved against</param>
/// <param name="configuredDirectory">Value of LanguagePlugins:Directory, null or blank when not configured</param>

using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.Klacksy;

public static class LanguagePluginDirectoryResolver
{
    public static string Resolve(string baseDirectory, string? configuredDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? LanguagePluginConstants.PluginDirectory
            : configuredDirectory;

        return Path.IsPathRooted(directory) ? directory : Path.Combine(baseDirectory, directory);
    }
}
