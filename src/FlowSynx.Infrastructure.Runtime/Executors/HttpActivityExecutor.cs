using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Workflows;
using FlowSynx.Infrastructure.Runtime.Execution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Executors;

public class HttpActivityExecutor : BaseActivityExecutor
{
    private readonly HttpClient _httpClient;

    public HttpActivityExecutor(ILogger<HttpActivityExecutor> logger) : base(logger)
    {
        _httpClient = new HttpClient();
    }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == ExecutableComponentType.Http;
    }

    public override async Task<object> ExecuteAsync(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var http = activity.Spec.Executable.Http;
        if (http == null)
        {
            throw new Exception("HTTP configuration is missing");
        }

        var url = http.Url;
        if (string.IsNullOrEmpty(url))
        {
            throw new Exception("HTTP URL is not specified");
        }

        try
        {
            var request = new HttpRequestMessage(
                GetHttpMethod(http.Method),
                url);

            // Add headers
            if (http.Headers != null)
            {
                foreach (var header in http.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add body
            if (parameters != null && parameters.Count > 0)
            {
                var json = JsonSerializer.Serialize(parameters);
                request.Content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");
            }

            _logger.LogInformation("Making HTTP request to {Url}", url);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // Try to parse as JSON
            try
            {
                return JsonSerializer.Deserialize<object>(responseBody);
            }
            catch
            {
                return responseBody;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP request failed to {Url}", http.Url);
            throw new Exception($"HTTP request failed: {ex.Message}", ex);
        }
    }

    private HttpMethod GetHttpMethod(HttpEndpointMethod method) => method switch
    {
        HttpEndpointMethod.GET => HttpMethod.Get,
        HttpEndpointMethod.POST => HttpMethod.Post,
        HttpEndpointMethod.PUT => HttpMethod.Put,
        HttpEndpointMethod.DELETE => HttpMethod.Delete,
        HttpEndpointMethod.PATCH => HttpMethod.Patch,
        HttpEndpointMethod.HEAD => HttpMethod.Head,
        HttpEndpointMethod.OPTIONS => HttpMethod.Options,
        _ => HttpMethod.Post
    };
}