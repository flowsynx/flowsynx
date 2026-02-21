using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Workflows;
using FlowSynx.Infrastructure.Runtime.Execution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Executors;

public class HttpActivityExecutor : BaseActivityExecutor
{
    private readonly System.Net.Http.HttpClient _httpClient;

    public HttpActivityExecutor(ILogger<HttpActivityExecutor> logger) : base(logger)
    {
        _httpClient = new System.Net.Http.HttpClient();
    }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "http";
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
            var request = new System.Net.Http.HttpRequestMessage(
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
                request.Content = new System.Net.Http.StringContent(
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

    private System.Net.Http.HttpMethod GetHttpMethod(string method)
    {
        return method?.ToUpper() switch
        {
            "GET" => System.Net.Http.HttpMethod.Get,
            "POST" => System.Net.Http.HttpMethod.Post,
            "PUT" => System.Net.Http.HttpMethod.Put,
            "DELETE" => System.Net.Http.HttpMethod.Delete,
            "PATCH" => System.Net.Http.HttpMethod.Patch,
            _ => System.Net.Http.HttpMethod.Post
        };
    }
}