using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Converters
{
    public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();

            string[] formats = { "HH:mm:ss", "HH:mm", "H:mm:ss", "H:mm" };

            foreach (var format in formats)
            {
                if (TimeOnly.TryParseExact(s, format, null, DateTimeStyles.None, out var timeOnly))
                {
                    return timeOnly;
                }
            }

            throw new JsonException($"Unable to parse '{s}' as TimeOnly");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("HH:mm:ss"));
        }
    }
}
