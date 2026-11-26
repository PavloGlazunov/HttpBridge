using System.Net;
using System.Text.Json;
using HttpBridge.Core;
using HttpBridge.Core.Models;
using HttpBridge.Core.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpBridge.Tests;

public class ResponseHelperTests
{
    private static HttpResponseMessage Response(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body) };

    [Fact]
    public async Task Transport_failure_is_unhealthy_not_an_exception()
    {
        HealthCheckResult result = await ResponseHelper.ReadHealthAsync(
            Task.FromException<HttpResponseMessage>(new HttpRequestException("refused")));

        Assert.Equal("Unhealthy", result.Status);
        Assert.Equal(["refused"], result.Errors);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "", "Healthy")]
    [InlineData(HttpStatusCode.OK, "Healthy", "Healthy")]
    [InlineData(HttpStatusCode.OK, "<html>proxy</html>", "Unhealthy")]
    [InlineData(HttpStatusCode.OK, "Degraded", "Unhealthy")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Unhealthy", "Unhealthy")]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"status":"Unhealthy","errors":["db"]}""", "Unhealthy")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "{}", "Unhealthy")]
    [InlineData(HttpStatusCode.BadRequest, """{"status":"Healthy"}""", "Healthy")]
    public async Task Health_status_is_mapped_and_never_null(HttpStatusCode code, string body, string expected)
    {
        HealthCheckResult result = await ResponseHelper.ReadHealthAsync(Task.FromResult(Response(code, body)));

        Assert.NotNull(result);
        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public async Task Invalid_json_surfaces_with_status_and_body()
    {
        var ex = await Assert.ThrowsAsync<JsonException>(() =>
            ResponseHelper.DeserializeResponseMessageAsync<HealthCheckResult>(Response(HttpStatusCode.OK, "<html>oops</html>")));

        Assert.Contains("HTTP 200", ex.Message);
        Assert.Contains("<html>oops</html>", ex.Message);
    }

    [Fact]
    public async Task Empty_body_is_default()
    {
        Assert.Null(await ResponseHelper.DeserializeResponseMessageAsync<HealthCheckResult>(Response(HttpStatusCode.OK, "")));
    }

    [Theory]
    [InlineData("https://host/api/v1", "https://host/api/v1/")]
    [InlineData("https://host/api/v1/", "https://host/api/v1/")]
    [InlineData("https://host", "https://host/")]
    public void BaseUrl_is_normalised_with_trailing_slash(string configured, string expected)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["Google:BaseUrl"] = configured })
            .Build();

        Assert.Equal(expected, new ServiceCollection().BindOptions<GoogleConfigurationOptions>(config).BaseUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a url")]
    [InlineData("/relative")]
    public void Missing_or_relative_BaseUrl_fails_at_registration(string configured)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(configured is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["Google:BaseUrl"] = configured })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().BindOptions<GoogleConfigurationOptions>(config));
        Assert.Contains("Google", ex.Message);
    }
}
