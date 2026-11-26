using System.Text.Json;
using System.Text.Json.Serialization;

namespace Http.Extensions.Core.Utils;

public static class JsonSerializationSettingsBuilder
{
    public static JsonSerializerOptions CreateDefaultJsonOptions()
    {
        JsonSerializerOptions options = new();
        SetDefaultJsonOptions(options);
        return options;
    }

    public static void SetDefaultJsonOptions(
        JsonSerializerOptions options,
        Action<JsonSerializerOptions> additionalConfigure = null)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        AddConverter(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        additionalConfigure?.Invoke(options);

        void AddConverter<T>(T converter = null) where T : JsonConverter, new()
        {
            if (!options.Converters.OfType<T>().Any())
            {
                options.Converters.Add(converter ?? new T());
            }
        }
    }
}
