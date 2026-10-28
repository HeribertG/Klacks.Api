// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Models the Playground instance is pinned to, bound to the configuration section <c>Playground:Models</c>
/// (environment: <c>Playground__Models__ChatModelId</c> etc.). They come from configuration and not from the
/// database because every visitor is the shared demo admin, who can edit the database. A missing or blank
/// value keeps the default.
/// </summary>
/// <param name="ChatModelId">llm_models.model_id used for every chat turn</param>
/// <param name="TranscriptionProviderId">llm_providers.provider_id used for the transcription cleanup</param>
/// <param name="TranscriptionApiModelId">Provider-side model id sent to that provider</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Playground;

public class PlaygroundModelOptions
{
    public string ChatModelId { get; set; } = PlaygroundHardeningConstants.DefaultChatModelId;

    public string TranscriptionProviderId { get; set; } = PlaygroundHardeningConstants.DefaultTranscriptionProviderId;

    public string TranscriptionApiModelId { get; set; } = PlaygroundHardeningConstants.DefaultTranscriptionApiModelId;

    /// <summary>
    /// Reads the three values from the section; a blank or absent value keeps the default.
    /// </summary>
    /// <param name="section">The Playground:Models configuration section</param>
    public void ApplyConfiguration(IConfiguration section)
    {
        ChatModelId = NonBlank(section[nameof(ChatModelId)], PlaygroundHardeningConstants.DefaultChatModelId);
        TranscriptionProviderId = NonBlank(section[nameof(TranscriptionProviderId)], PlaygroundHardeningConstants.DefaultTranscriptionProviderId);
        TranscriptionApiModelId = NonBlank(section[nameof(TranscriptionApiModelId)], PlaygroundHardeningConstants.DefaultTranscriptionApiModelId);
    }

    /// <param name="value">The configured value, null or blank when unset</param>
    /// <param name="fallback">Default used when the value is blank</param>
    public static string NonBlank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
