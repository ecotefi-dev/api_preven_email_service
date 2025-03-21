using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Text.Json;

namespace api_preven_email_service.Helper
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string[] formats = ["dd/MM/yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd"];

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (DateTime.TryParseExact(reader.GetString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return date;
            }
            throw new JsonException($"Formato de fecha inválido: {reader.GetString()}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss"));
        }
    }
}
