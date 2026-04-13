// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Represents a user-configured or pre-installed speech-to-text provider.
/// </summary>
/// <param name="Name">Display name for the provider</param>
/// <param name="ConnectionType">'websocket' or 'rest'</param>
/// <param name="ApiUrl">Endpoint URL for the STT service</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class CustomSttProvider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "rest";
    public string ApiUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? LanguageModel { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSystem { get; set; }
}
