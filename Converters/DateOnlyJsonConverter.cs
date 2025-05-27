using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Converters;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var s = reader.GetString()
                ?? throw new JsonException("Expected date string");
        return DateOnly.ParseExact(s, Format, null);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
