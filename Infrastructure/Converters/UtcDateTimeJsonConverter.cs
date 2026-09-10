// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads request DateTime values and refuses ones that carry a non-zero UTC offset.
///
/// System.Text.Json turns "2026-09-10T00:00:00+02:00" into a DateTime of Kind Local, and Npgsql
/// refuses to write Kind Local into a timestamp-with-time-zone column, so such a request used to
/// fail deep inside the save as a "Database constraint violation". Converting it to UTC is not safe
/// either: date-like fields (ValidFrom, ...) are stored as UTC midnight of the calendar day, which is
/// what the frontend sends, and local midnight at +02:00 would silently become the previous day. The
/// client's intended calendar day is already lost once the offset has been applied. So the value is
/// rejected with a message that says what to send instead.
///
/// "Z" and "+00:00" are accepted as UTC. Values without any offset keep their previous behaviour
/// (Kind Unspecified); inferring a time zone for them would be guesswork. Writing is unchanged.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Converters;

public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public const string OffsetNotSupportedMessage =
        "Date/time values must be sent in UTC, e.g. '2026-09-10T00:00:00Z'. A value with a non-zero UTC "
        + "offset is rejected because a calendar date sent as local midnight would otherwise be stored "
        + "as the previous day.";

    private const string InvalidFormatMessage = "The value is not a valid ISO 8601 date/time.";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTime(out var value))
        {
            throw new JsonException(InvalidFormatMessage);
        }

        if (value.Kind != DateTimeKind.Local)
        {
            return value;
        }

        if (reader.TryGetDateTimeOffset(out var withOffset) && withOffset.Offset == TimeSpan.Zero)
        {
            return withOffset.UtcDateTime;
        }

        throw new JsonException(OffsetNotSupportedMessage);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
