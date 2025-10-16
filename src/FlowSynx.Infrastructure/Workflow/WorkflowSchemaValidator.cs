using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.Infrastructure.Serialization;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Net.Http.Headers;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowSchemaValidator : IWorkflowSchemaValidator
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(12);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<WorkflowSchemaValidator> _logger;
    private readonly Application.Localizations.ILocalization _localization;

    public WorkflowSchemaValidator(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        ILogger<WorkflowSchemaValidator> logger,
        Application.Localizations.ILocalization localization)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public async Task ValidateAsync(string? schemaUrl, string definitionJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(schemaUrl))
            return;

        if (!Uri.TryCreate(schemaUrl, UriKind.Absolute, out var schemaUri))
        {
            throw new FlowSynxException(
                (int)ErrorCode.WorkflowSchemaInvalidUrl,
                _localization.Get("Workflow_Schema_InvalidUrl", schemaUrl));
        }

        var schema = await GetSchemaAsync(schemaUri, cancellationToken);
        var sanitized = JsonSanitizer.Sanitize(definitionJson);

        try
        {
            using var reader = new JsonTextReader(new StringReader(sanitized));
            var token = await JToken.LoadAsync(reader, cancellationToken);

            if (!token.IsValid(schema, out IList<string> errors))
            {
                var joinedErrors = string.Join("; ", errors);
                _logger.LogWarning("Workflow schema validation failed for {SchemaUrl}: {Errors}", schemaUrl, joinedErrors);
                throw new FlowSynxException(
                    (int)ErrorCode.WorkflowSchemaValidationFailed,
                    _localization.Get("Workflow_Schema_ValidationFailed", joinedErrors));
            }
        }
        catch (JsonException ex)
        {
            throw new FlowSynxException(
                (int)ErrorCode.Serialization,
                _localization.Get("Workflow_Schema_InvalidJsonPayload", ex.Message));
        }
    }

    private async Task<JSchema> GetSchemaAsync(Uri schemaUri, CancellationToken cancellationToken)
    {
        return await _memoryCache.GetOrCreateAsync(schemaUri, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;
            var client = _httpClientFactory.CreateClient(nameof(WorkflowSchemaValidator));

            using var request = new HttpRequestMessage(HttpMethod.Get, schemaUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/schema+json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new FlowSynxException(
                    (int)ErrorCode.WorkflowSchemaFetchFailed,
                    _localization.Get("Workflow_Schema_FetchFailed", (int)response.StatusCode, schemaUri));
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                return JSchema.Parse(payload);
            }
            catch (JSchemaException ex)
            {
                throw new FlowSynxException(
                    (int)ErrorCode.WorkflowSchemaInvalidSchemaPayload,
                    _localization.Get("Workflow_Schema_InvalidSchemaPayload", ex.Message));
            }
        }) ?? throw new FlowSynxException(
            (int)ErrorCode.WorkflowSchemaFetchFailed,
            _localization.Get("Workflow_Schema_FetchFailed", 0, schemaUri));
    }
}
