using System.Net.Http.Json;
using System.Text.Json;

namespace RestEase.Extensions;

/// <summary>RestEase ships Json.NET only; these plug System.Text.Json into <c>RestClient</c>.</summary>
public sealed class SystemTextJsonRequestBodySerializer(JsonSerializerOptions options) : RequestBodySerializer
{
    public override HttpContent SerializeBody<T>(T body, RequestBodySerializerInfo info) =>
        body is null ? null : JsonContent.Create(body, options: options);
}

public sealed class SystemTextJsonResponseDeserializer(JsonSerializerOptions options) : ResponseDeserializer
{
    public override T Deserialize<T>(string content, HttpResponseMessage response, ResponseDeserializerInfo info) =>
        string.IsNullOrEmpty(content) ? default : JsonSerializer.Deserialize<T>(content, options);
}
