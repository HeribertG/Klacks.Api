// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Return path of the pseudonymisation: puts the real values back into what the model wrote. Accepts the canonical
/// form (MA-0017, MA-0017-N) and tolerant spellings (ma-17, MA0017, MA-0017s with the ending kept), but a tolerant
/// spelling only when its number exists, so ordinary text never turns into a name. A space is never accepted between
/// prefix and number ("MA 3" is German shorthand for three employees). An unknown token in canonical form stays in
/// answer text and is reported; in tool arguments it is replaced by <see cref="PseudonymizationConstants.UnknownReferenceMarker"/>
/// so the chat loop rejects the call. In JSON arguments a string that is exactly a personnel-number token becomes a number.
/// </summary>

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static partial class PseudonymRestorer
{
    /// <param name="text">Text written by the model</param>
    /// <param name="map">Dictionary and pattern numbers of the request</param>
    /// <param name="unknownTokens">Collects canonical tokens that do not exist</param>
    /// <param name="isToolArgument">True inside tool arguments, where an unknown token is marked for rejection</param>
    public static string RestoreText(string? text, PseudonymMap map, ICollection<string> unknownTokens, bool isToolArgument = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return TokenPattern().Replace(text, match =>
        {
            if (!TryParse(match, out var token))
            {
                return match.Value;
            }

            var canonical = token.ToString();
            if (map.TryRestore(canonical, out var surface))
            {
                return surface;
            }

            if (!string.Equals(match.Value, canonical, StringComparison.Ordinal))
            {
                return match.Value;
            }

            unknownTokens.Add(canonical);
            return isToolArgument ? PseudonymizationConstants.UnknownReferenceMarker + canonical : match.Value;
        });
    }

    /// <summary>
    /// Restores every string value of a JSON argument object. Unparseable JSON is returned unchanged: the caller's
    /// parser rejects it anyway, and string-replacing inside raw JSON could break its escaping.
    /// </summary>
    public static string RestoreJsonArguments(string json, PseudonymMap map, ICollection<string> unknownTokens)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return json;
        }

        return node == null ? json : RestoreNode(node, map, unknownTokens)?.ToJsonString() ?? json;
    }

    /// <summary>
    /// Restores the parsed parameters of a non-streamed tool call into a new dictionary.
    /// </summary>
    public static Dictionary<string, object> RestoreParameters(
        Dictionary<string, object> parameters, PseudonymMap map, ICollection<string> unknownTokens)
    {
        var restored = new Dictionary<string, object>(parameters.Count, parameters.Comparer);
        foreach (var (key, value) in parameters)
        {
            restored[key] = RestoreValue(value, map, unknownTokens)!;
        }

        return restored;
    }

    private static object? RestoreValue(object? value, PseudonymMap map, ICollection<string> unknownTokens)
    {
        switch (value)
        {
            case string text:
                return RestoreText(text, map, unknownTokens, isToolArgument: true);
            case JsonElement element:
                var node = JsonSerializer.SerializeToNode(element);
                return node == null ? element : JsonSerializer.SerializeToElement(RestoreNode(node, map, unknownTokens));
            case Dictionary<string, object> nested:
                return RestoreParameters(nested, map, unknownTokens);
            case List<object> list:
                return list.Select(item => RestoreValue(item, map, unknownTokens)!).ToList();
            default:
                return value;
        }
    }

    private static JsonNode? RestoreNode(JsonNode node, PseudonymMap map, ICollection<string> unknownTokens)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (property.Value != null)
                    {
                        obj[property.Key] = RestoreNode(property.Value.DeepClone(), map, unknownTokens);
                    }
                }

                return obj;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (array[i] != null)
                    {
                        array[i] = RestoreNode(array[i]!.DeepClone(), map, unknownTokens);
                    }
                }

                return array;
            case JsonValue value when value.TryGetValue<string>(out var text):
                if (IsPersonnelNumberToken(text, map, out var number))
                {
                    return JsonValue.Create(number);
                }

                return JsonValue.Create(RestoreText(text, map, unknownTokens, isToolArgument: true));
            default:
                return node;
        }
    }

    private static bool IsPersonnelNumberToken(string text, PseudonymMap map, out int number)
    {
        number = 0;
        var match = TokenPattern().Match(text.Trim());
        return match.Success
            && match.Length == text.Trim().Length
            && TryParse(match, out var token)
            && string.Equals(token.Variant, PseudonymizationConstants.PersonnelNumberVariant, StringComparison.Ordinal)
            && map.TryRestore(token.ToString(), out var surface)
            && int.TryParse(surface, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }

    private static bool TryParse(Match match, out PseudonymToken token)
    {
        token = default;
        if (!PseudonymPrefixes.TryGetKind(match.Groups["prefix"].Value, out PseudonymKind kind)
            || !int.TryParse(match.Groups["num"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var ordinal))
        {
            return false;
        }

        var variant = match.Groups["var"].Success ? match.Groups["var"].Value.ToUpperInvariant() : null;
        token = new PseudonymToken(kind, ordinal, variant);
        return true;
    }

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?<prefix>NAME|MAIL|TEL|MA|KL|BE)(?:-(?<num>\d{1,8})|(?<num>\d{4,8}))(?!\d)(?:-(?<var>[A-Za-z]\d?)(?![\p{L}\p{N}]))?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    internal static partial Regex TokenPattern();
}
