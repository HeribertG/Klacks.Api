// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

public class AnthropicContent
{
    public string Type { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? Name { get; set; }
    public Dictionary<string, object>? Input { get; set; }
}