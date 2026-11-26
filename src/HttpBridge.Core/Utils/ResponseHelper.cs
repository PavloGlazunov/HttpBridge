using System.Text.Json;
using HttpBridge.Core.Models;

namespace HttpBridge.Core.Utils;

public static class ResponseHelper
{
    public static async Task<T> DeserializeResponseMessageAsync<T>(
        HttpResponseMessage responseMessage,
        JsonSerializerOptions options = null)
    {
        await using Stream stream = await responseMessage.Content.ReadAsStreamAsync();
        try
        {
            var response = await JsonSerializer.DeserializeAsync<T>(
                stream,
                options ?? JsonSerializationSettingsBuilder.CreateDefaultJsonOptions());
            return response;
        }
        catch (Exception)
        {
            // ignored
        }

        return default;
    }

    public static async Task<T> GetResponseAsync<T>(
        Task<HttpResponseMessage> responseTask,
        JsonSerializerOptions options = null)
    {
        HttpResponseMessage responseMessage = await responseTask;
        if (!responseMessage.IsSuccessStatusCode)
        {
            string content = await responseMessage.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Error: {responseMessage.StatusCode}, Content: {content}",
                null,
                responseMessage.StatusCode);
        }

        return await DeserializeResponseMessageAsync<T>(responseMessage, options);
    }

    public static async Task GetResponseAsync(Task<HttpResponseMessage> responseTask)
    {
        HttpResponseMessage responseMessage = await responseTask;
        if (!responseMessage.IsSuccessStatusCode)
        {
            string content = await responseMessage.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Error: {responseMessage.StatusCode}, Content: {content}",
                null,
                responseMessage.StatusCode);
        }
    }

    /// <summary>200 or 400 -> deserialized <see cref="HealthCheckResult"/>, anything else -> null.</summary>
    public static async Task<HealthCheckResult> ReadHealthAsync(Task<HttpResponseMessage> responseTask)
    {
        HttpResponseMessage response = await responseTask;
        return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest
            ? await DeserializeResponseMessageAsync<HealthCheckResult>(response)
            : null;
    }
}
