// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wire request DTO for the OpenAI Chat Completions API (legacy function_call format).
/// Temperature is nullable because OpenAI's reasoning-oriented models reject a caller-supplied
/// temperature (e.g. gpt-5-nano, gpt-5-search-api return HTTP 400); a null value omits the field
/// from the payload entirely so the API applies its own default.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }
    
    [JsonPropertyName("max_completion_tokens")]
    public int MaxTokens { get; set; }
    
    [JsonPropertyName("functions")]
    public List<OpenAIFunction>? Functions { get; set; }
    
    [JsonPropertyName("function_call")]
    public string? FunctionCall { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stream { get; set; }

    /// <summary>
    /// Asks the API to append a final chunk carrying the token counters, which a stream otherwise
    /// never reports. Null omits the field, because not every OpenAI-compatible endpoint accepts it
    /// and an unknown field fails the whole request with HTTP 400.
    /// </summary>
    [JsonPropertyName("stream_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIStreamOptions? StreamOptions { get; set; }
}

public class OpenAIStreamOptions
{
    [JsonPropertyName("include_usage")]
    public bool IncludeUsage { get; set; }
}