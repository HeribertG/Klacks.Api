using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Converters;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (DateTime.TryParse(s, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return DateOnly.ParseExact(s, Format, null);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}
