using System.Net;
using System.Text.Json;
using HttpBridge.Core.Models;

namespace HttpBridge.Core.Utils;

public static class ResponseHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = JsonSerializationSettingsBuilder.CreateDefaultJsonOptions();

    /// <summary>Empty body -> default. A body that is not valid JSON for <typeparamref name="T"/> throws <see cref="JsonException"/> with the status and the start of the body.</summary>
    public static async Task<T> DeserializeResponseMessageAsync<T>(
        HttpResponseMessage responseMessage,
        JsonSerializerOptions options = null)
    {
        string body = await responseMessage.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, options ?? DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException(
                $"Cannot deserialize {typeof(T).Name} from HTTP {(int)responseMessage.StatusCode} response: {Truncate(body)}",
                ex);
        }
    }

    public static async Task<T> GetResponseAsync<T>(
        Task<HttpResponseMessage> responseTask,
        JsonSerializerOptions options = null)
    {
        HttpResponseMessage responseMessage = await responseTask;
        await EnsureSuccessAsync(responseMessage);
        return await DeserializeResponseMessageAsync<T>(responseMessage, options);
    }

    public static async Task GetResponseAsync(Task<HttpResponseMessage> responseTask)
    {
        HttpResponseMessage responseMessage = await responseTask;
        await EnsureSuccessAsync(responseMessage);
    }

    /// <summary>Never throws and never returns null: transport errors and non-success statuses become an unhealthy result.</summary>
    public static Task<HealthCheckResult> ReadHealthAsync(Task<HttpResponseMessage> responseTask) =>
        ProbeAsync(async () =>
        {
            using HttpResponseMessage response = await responseTask;
            return ToHealthResult(response.StatusCode, await response.Content.ReadAsStringAsync());
        });

    /// <summary>Runs a health probe; any exception (timeout, open circuit, connection refused, ...) becomes an unhealthy result.</summary>
    public static async Task<HealthCheckResult> ProbeAsync(Func<Task<HealthCheckResult>> probe)
    {
        try
        {
            return await probe();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.CreateUnhealthyResult([ex.Message]);
        }
    }

    /// <summary>
    /// JSON body with a "status" property wins (also for 4xx/5xx); otherwise 2xx is healthy and anything else is unhealthy.
    /// Plain-text bodies such as ASP.NET Core's default "Healthy" / "Degraded" / "Unhealthy" are reported as errors.
    /// </summary>
    public static HealthCheckResult ToHealthResult(HttpStatusCode status, string body)
    {
        string text = body?.Trim();
        if (!string.IsNullOrEmpty(text) && text[0] == '{' && TryParseHealth(text) is { } parsed)
        {
            return parsed;
        }

        if ((int)status is >= 200 and < 300 && (string.IsNullOrEmpty(text) || text == HealthCheckResult.HealthyStatus))
        {
            return HealthCheckResult.Healthy;
        }

        string reason = string.IsNullOrEmpty(text) || text[0] == '{' || text[0] == '<' ? "" : $": {Truncate(text)}";
        return HealthCheckResult.CreateUnhealthyResult([$"HTTP {(int)status}{reason}"]);
    }

    private static HealthCheckResult TryParseHealth(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            bool hasStatus = document.RootElement.EnumerateObject()
                .Any(p => p.Name.Equals("status", StringComparison.OrdinalIgnoreCase));
            return hasStatus ? document.RootElement.Deserialize<HealthCheckResult>(DefaultOptions) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage responseMessage)
    {
        if (!responseMessage.IsSuccessStatusCode)
        {
            string content = await responseMessage.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Error: {responseMessage.StatusCode}, Content: {content}",
                null,
                responseMessage.StatusCode);
        }
    }

    private static string Truncate(string text) => text.Length <= 200 ? text : text[..200] + "...";
}
