// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared low-level UTC date/time read grammar, extracted out of UtcDateTimeJsonConverter so
/// DateOnlyStringParser can read the same wire shapes instead of maintaining its own format list that
/// could silently drift apart (accepting or rejecting different strings, or resolving an offset value
/// to a different calendar day). Behavior is unchanged from UtcDateTimeJsonConverter.Read.
/// </summary>

using System.Text.Json;

namespace Klacks.Api.Infrastructure.Converters;

internal static class UtcDateTimeReader
{
    internal const string InvalidFormatMessage = "The value is not a valid ISO 8601 date/time.";

    public static DateTime ReadUtc(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTime(out var value))
        {
            throw new JsonException(InvalidFormatMessage);
        }

        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        if (reader.TryGetDateTimeOffset(out var withOffset) && withOffset.Offset == TimeSpan.Zero)
        {
            return withOffset.UtcDateTime;
        }

        throw new JsonException(UtcDateTimeJsonConverter.OffsetNotSupportedMessage);
    }
}
