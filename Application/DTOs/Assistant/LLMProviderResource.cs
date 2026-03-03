// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMProviderResource
{
    public Guid Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool HasApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiVersion { get; set; }
    public int Priority { get; set; }
}
