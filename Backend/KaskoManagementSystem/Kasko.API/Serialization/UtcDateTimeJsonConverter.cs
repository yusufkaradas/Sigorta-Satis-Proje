using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kasko.API.Serialization;

public sealed class UtcDateTimeJsonConverter
    : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException(
                "Geçersiz tarih değeri.");
        }

        if (value.EndsWith(
                "Z",
                StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.Parse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal);
        }

        var hasOffset =
            value.Length >= 6 &&
            (value[^6] == '+' || value[^6] == '-') &&
            value[^3] == ':';

        if (hasOffset)
        {
            return DateTimeOffset.Parse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None)
                .UtcDateTime;
        }

        var unspecified = DateTime.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

        return DateTime.SpecifyKind(
            unspecified,
            DateTimeKind.Utc);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc =>
                value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc)
        };

        writer.WriteStringValue(utcValue);
    }
}