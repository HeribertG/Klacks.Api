using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Converters;

public class DateOnlyNullableJsonConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var s = reader.GetString();
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }

        if (DateTime.TryParse(s, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        if (DateOnly.TryParse(s, out var dateOnly))
        {
            return dateOnly;
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
