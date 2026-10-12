// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Content fingerprint of the turn-intent labelset that a trained head records as its DatasetHash. Hashes only what
/// training reads (id, locale, label, ambiguity, NFC text), sorted by id, so formatting, key order, line endings and
/// review-only fields do not change it while any change to a training example does.
/// </summary>
/// <param name="json">Content of turn-intent-v1.json</param>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Klacks.Api.Application.Klacksy;

public static class TurnIntentLabelsetFingerprint
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string LocaleProperty = "locale";
    private const string LabelProperty = "label";
    private const string TextProperty = "text";
    private const string AmbiguousProperty = "ambiguous";
    private const char FieldSeparator = '\t';
    private const char LineSeparator = '\n';
    private const string AmbiguousMarker = "1";
    private const string UnambiguousMarker = "0";

    public static string Compute(string json)
    {
        using var document = JsonDocument.Parse(json);
        var lines = document.RootElement.GetProperty(ItemsProperty).EnumerateArray()
            .Select(item => string.Join(
                FieldSeparator,
                item.GetProperty(IdProperty).GetString(),
                item.GetProperty(LocaleProperty).GetString(),
                item.GetProperty(LabelProperty).GetString(),
                IsAmbiguous(item) ? AmbiguousMarker : UnambiguousMarker,
                (item.GetProperty(TextProperty).GetString() ?? string.Empty).Trim().Normalize(NormalizationForm.FormC)))
            .OrderBy(line => line, StringComparer.Ordinal);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(LineSeparator, lines)));
        return Convert.ToHexString(bytes).ToLower(CultureInfo.InvariantCulture);
    }

    private static bool IsAmbiguous(JsonElement item) =>
        item.TryGetProperty(AmbiguousProperty, out var ambiguous) && ambiguous.ValueKind == JsonValueKind.True;
}
