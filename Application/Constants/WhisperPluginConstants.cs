// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the self-hosted Whisper STT plugin (install/uninstall via the updater hand-off queue).
/// The provider id is the well-known row in custom_stt_providers shared with deploy/whisper-stt-provider.sql;
/// model aliases are what the UI sends, model ids are what Speaches downloads from Hugging Face.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class WhisperPluginConstants
{
    public static readonly Guid ProviderId = Guid.Parse("7f3d2a10-0000-4000-8000-57705731a001");

    public const string ProviderName = "Whisper (self-hosted)";

    public const string ProviderApiUrl = "http://whisper-stt:8000";

    public const string ModelAliasSmall = "small";

    public const string ModelAliasLarge = "large-v3-turbo";

    public const string SmallModelId = "Systran/faster-whisper-small";

    public const string LargeModelId = SttProviderConstants.DefaultCustomWhisperModel;

    public static string? ResolveModelId(string? modelAlias) => modelAlias switch
    {
        ModelAliasSmall => SmallModelId,
        ModelAliasLarge => LargeModelId,
        _ => null,
    };

    public static string? ResolveModelAlias(string? modelId) => modelId switch
    {
        SmallModelId => ModelAliasSmall,
        LargeModelId => ModelAliasLarge,
        _ => null,
    };
}
