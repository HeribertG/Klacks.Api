// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Extracts recipe slot values from the opening message in a single structured LLM call, so a recipe
/// does not re-ask for something the user already stated. This is the only place the model has latitude;
/// every later step stays deterministically forced. Any failure (timeout, non-JSON, parse error)
/// degrades to an empty result so all ask steps simply fire — the call never throws into the turn.
/// </summary>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant;

public class RecipeSlotExtractor
{
    private const string SystemPrompt =
        "You extract structured field values from a single user message. " +
        "Return ONLY a compact JSON object, no prose, no code fences. " +
        "Include a key only when its value is clearly present in the message; omit everything else.";

    private const double ExtractionTemperature = 0.0;

    // Generous budget: Gemini 3.x counts thinking tokens against maxOutputTokens, and a tight limit
    // leaves only a truncated fragment of the JSON (observed: a tiny tail like '6" -> "2026-0').
    private const int ExtractionMaxTokens = 2000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<RecipeSlotExtractor> _logger;

    public RecipeSlotExtractor(ILogger<RecipeSlotExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> ExtractAsync(
        ILLMProvider provider,
        LLMModel model,
        string message,
        IReadOnlyDictionary<string, string> slotHints,
        CancellationToken cancellationToken = default)
    {
        var empty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (slotHints.Count == 0 || string.IsNullOrWhiteSpace(message))
        {
            return empty;
        }

        try
        {
            var request = new LLMProviderRequest
            {
                Message = BuildPrompt(message, slotHints),
                SystemPrompt = SystemPrompt,
                ModelId = model.ApiModelId,
                Temperature = ExtractionTemperature,
                MaxTokens = ExtractionMaxTokens,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken
            };

            var response = await provider.ProcessAsync(request);
            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                return empty;
            }

            var parsed = Parse(response.Content, slotHints.Keys);
            _logger.LogDebug("Recipe slot extraction filled {Count} slot(s): [{Slots}]",
                parsed.Count, string.Join(", ", parsed.Keys));
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recipe slot extraction failed; degrading to no pre-filled slots.");
            return empty;
        }
    }

    private static string BuildPrompt(string message, IReadOnlyDictionary<string, string> slotHints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Extract these fields if clearly present (omit any that are not):");
        foreach (var (slot, hint) in slotHints)
        {
            sb.AppendLine($"- {slot}: {hint}");
        }

        sb.AppendLine();
        sb.AppendLine($"Message: \"{message}\"");
        sb.AppendLine();
        sb.Append("Return a JSON object with only the present fields.");
        return sb.ToString();
    }

    private static Dictionary<string, string> Parse(string content, IEnumerable<string> allowedSlots)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return result;
        }

        var json = content[start..(end + 1)];
        var allowed = new HashSet<string>(allowedSlots, StringComparer.OrdinalIgnoreCase);

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (!allowed.Contains(property.Name))
                {
                    continue;
                }

                var value = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    result[property.Name] = value!;
                }
            }
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return result;
    }
}
