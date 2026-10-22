// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants for the self-hosted STT plugin (install/uninstall via the updater hand-off queue). One provider
/// row serves all engines (Whisper small, Whisper large-v3-turbo, Parakeet): the updater rewrites its name,
/// url and model per engine. The provider id is the well-known row in custom_stt_providers shared with
/// deploy/whisper-stt-provider.sql; model aliases are what the UI sends, model ids are what Speaches
/// downloads from Hugging Face (Whisper) or what Klacks.Stt reports as installed (Parakeet).
/// </summary>
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.Constants;

public static class WhisperPluginConstants
{
    public static readonly Guid ProviderId = Guid.Parse("7f3d2a10-0000-4000-8000-57705731a001");

    public const string ProviderName = "Whisper (self-hosted)";

    public const string ParakeetProviderName = "Parakeet (self-hosted)";

    public const string ProviderApiUrl = "http://whisper-stt:8000";

    public const string ModelAliasSmall = "small";

    public const string ModelAliasLarge = "large-v3-turbo";

    public const string ModelAliasParakeet = "parakeet-v3";

    public const string SmallModelId = "Systran/faster-whisper-small";

    public const string LargeModelId = SttProviderConstants.DefaultCustomWhisperModel;

    public const string ParakeetModelId = "parakeet-tdt-0.6b-v3-int8";

    public const string LocalEngineValue = "local";

    private static readonly IReadOnlyList<string> NativeAliases = Array.AsReadOnly([ModelAliasParakeet]);

    private static readonly IReadOnlyList<string> AllAliases =
        Array.AsReadOnly([ModelAliasParakeet, ModelAliasSmall, ModelAliasLarge]);

    public static string ProviderEngineValue =>
        $"{SttProviderConstants.CustomProviderPrefix}{ProviderId}";

    public static IReadOnlyList<string> GetSupportedAliases(InstallationKind kind) =>
        kind == InstallationKind.Native ? NativeAliases : AllAliases;

    public static bool IsSupportedOn(InstallationKind kind, string? modelAlias) =>
        modelAlias is not null && GetSupportedAliases(kind).Contains(modelAlias);

    public static string GetDefaultAlias(InstallationKind kind) => GetSupportedAliases(kind)[0];

    public static string? ResolveModelId(string? modelAlias) => modelAlias switch
    {
        ModelAliasSmall => SmallModelId,
        ModelAliasLarge => LargeModelId,
        ModelAliasParakeet => ParakeetModelId,
        _ => null,
    };

    public static string? ResolveModelAlias(string? modelId) => modelId switch
    {
        SmallModelId => ModelAliasSmall,
        LargeModelId => ModelAliasLarge,
        ParakeetModelId => ModelAliasParakeet,
        _ => null,
    };
}
