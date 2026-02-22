// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere;

public class CohereRequest
{
    public string Model { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<CohereChatMessage> ChatHistory { get; set; } = new();
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
}