// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Data transfer object for a custom speech-to-text provider configuration.
/// </summary>
/// <param name="Id">Unique provider identifier</param>
/// <param name="Name">Display name for the provider</param>
/// <param name="ConnectionType">'websocket' or 'rest'</param>
/// <param name="ApiUrl">Endpoint URL for the STT service</param>
/// <param name="ApiKey">API key (masked as '***' in responses)</param>
/// <param name="LanguageModel">Optional language model identifier</param>
/// <param name="IsEnabled">Whether the provider is active</param>
/// <param name="IsSystem">Whether this is a pre-installed system provider</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record CustomSttProviderDto(
    Guid Id,
    string Name,
    string ConnectionType,
    string ApiUrl,
    string? ApiKey,
    string? LanguageModel,
    bool IsEnabled,
    bool IsSystem);
