// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMProviderResource
{
    public Guid Id { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool HasApiKey { get; set; }
    public bool RequiresApiKey { get; set; } = true;
    public string? BaseUrl { get; set; }
    public string? ApiVersion { get; set; }
    public int Priority { get; set; }
}
