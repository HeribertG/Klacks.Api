// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class ProviderCandidateResource
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiVersion { get; set; }
    public bool RequiresApiKey { get; set; } = true;
    public string? DocsUrl { get; set; }
    public ProviderCandidateSource Source { get; set; }
    public ProviderConnectivityStatus Connectivity { get; set; }
}
