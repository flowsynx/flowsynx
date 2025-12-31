using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlowSynx.Domain.Tenants;

public sealed class TenantConfiguration
{
    private readonly JsonObject _settings;

    // Default constructor
    public TenantConfiguration()
    {
        _settings = new JsonObject();
    }

    // Constructor from dictionary
    public TenantConfiguration(Dictionary<string, object> settings)
    {
        _settings = JsonSerializer.SerializeToNode(settings) as JsonObject
            ?? throw new DomainException("Invalid configuration settings");
    }

    // Constructor from JSON
    public TenantConfiguration(JsonObject settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    // Factory method for default settings
    public static TenantConfiguration Default()
    {
        var defaultSettings = new Dictionary<string, object>
        {
            ["Features"] = new Dictionary<string, object>
            {
                ["UserManagement"] = true,
                ["Reporting"] = false,
                ["AdvancedAnalytics"] = false,
                ["ApiAccess"] = true
            },
            ["RateLimiting"] = new Dictionary<string, object>
            {
                ["WindowSeconds"] = 60,
                ["PermitLimit"] = 100,
                ["QueueLimit"] = 10
            },
            ["Storage"] = new Dictionary<string, object>
            {
                ["MaxSizeLimit"] = 209715200,
                ["ResultStorage"] = new Dictionary<string, object>
                {
                    ["DefaultProvider"] = "local",
                    ["Providers"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["Name"] = "local",
                            ["Configuration"] = new Dictionary<string, object>
                            {
                                ["BasePath"] = "flowsynxresults/"
                            }
                        }
                    }
                }
            },
            ["Localization"] = new Dictionary<string, object>
            {
                ["Language"] = "en"
            },
            ["Security"] = new Dictionary<string, object>
            {
                ["Authentication"] = new Dictionary<string, object>
                {
                    ["Enabled"] = false,
                    ["Basic"] = new Dictionary<string, object>
                    {
                        ["Enabled"] = false,
                        ["Users"] = new object[0]
                    },
                    ["JwtProviders"] = new object[0]
                }
            },
            ["Cors"] = new Dictionary<string, object>
            {
                ["PolicyName"] = "FlowSynxCorsPolicy",
                ["AllowedOrigins"] = new[] { "*" },
                ["AllowCredentials"] = false
            }
        };

        return new TenantConfiguration(defaultSettings);
    }

    // Get value with type safety
    public T GetValue<T>(string key, T defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return defaultValue;

        var keys = key.Split(':');
        JsonNode currentNode = _settings;

        foreach (var k in keys)
        {
            if (currentNode is JsonObject obj && obj.TryGetPropertyValue(k, out var value))
            {
                currentNode = value;
            }
            else
            {
                return defaultValue;
            }
        }

        try
        {
            var json = currentNode?.ToJsonString();
            if (string.IsNullOrEmpty(json))
                return defaultValue;

            return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    // Get nested configuration section
    public TenantConfiguration GetSection(string key)
    {
        var value = GetValue<JsonObject>(key);
        return value != null ? new TenantConfiguration(value) : new TenantConfiguration();
    }

    // Set value
    public TenantConfiguration SetValue(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Configuration key cannot be empty");

        var keys = key.Split(':');
        var clone = Clone();
        JsonNode currentNode = clone._settings;

        // Navigate to parent node
        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (currentNode is JsonObject obj)
            {
                if (!obj.TryGetPropertyValue(keys[i], out var nextNode))
                {
                    nextNode = new JsonObject();
                    obj[keys[i]] = nextNode;
                }
                currentNode = nextNode;
            }
            else
            {
                throw new DomainException($"Invalid configuration path: {key}");
            }
        }

        // Set the value
        if (currentNode is JsonObject parent)
        {
            var lastKey = keys.Last();
            parent[lastKey] = JsonSerializer.SerializeToNode(value);
        }

        return clone;
    }

    // Merge with other settings (other overrides current)
    public TenantConfiguration Merge(TenantConfiguration other)
    {
        var currentDict = ToDictionary();
        var otherDict = other.ToDictionary();

        foreach (var kvp in otherDict)
        {
            currentDict[kvp.Key] = kvp.Value;
        }

        return new TenantConfiguration(currentDict);
    }

    // Convert to dictionary
    public Dictionary<string, object> ToDictionary()
    {
        return FlattenJsonObject(_settings);
    }

    private Dictionary<string, object> FlattenJsonObject(JsonObject obj, string prefix = "")
    {
        var result = new Dictionary<string, object>();

        foreach (var property in obj)
        {
            var key = string.IsNullOrEmpty(prefix) ? property.Key : $"{prefix}.{property.Key}";

            if (property.Value is JsonObject nestedObj)
            {
                var nested = FlattenJsonObject(nestedObj, key);
                foreach (var nestedKvp in nested)
                {
                    result[nestedKvp.Key] = nestedKvp.Value;
                }
            }
            else if (property.Value is JsonArray array)
            {
                result[key] = array.ToJsonString();
            }
            else
            {
                result[key] = property.Value?.ToString() ?? string.Empty;
            }
        }

        return result;
    }

    // Clone settings
    public TenantConfiguration Clone()
    {
        var json = _settings.ToJsonString();
        var cloned = JsonSerializer.Deserialize<JsonObject>(json);
        return new TenantConfiguration(cloned ?? new JsonObject());
    }

    // Validate settings
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate rate limiting
        var windowSeconds = GetValue<int>("RateLimiting.WindowSeconds");
        var permitLimit = GetValue<int>("RateLimiting.PermitLimit");

        if (windowSeconds < 1 || windowSeconds > 3600)
            errors.Add("Rate limiting window must be between 1 and 3600 seconds");

        if (permitLimit < 1 || permitLimit > 10000)
            errors.Add("Rate limiting permit limit must be between 1 and 10000");

        // Validate storage
        var maxSize = GetValue<long>("Storage.MaxSizeLimit");
        if (maxSize < 0)
            errors.Add("Storage max size cannot be negative");

        // Validate language
        var language = GetValue<string>("Localization.Language");
        var validLanguages = new[] { "en", "fr", "es", "de", "zh" };
        if (!validLanguages.Contains(language?.ToLower()))
            errors.Add($"Invalid language: {language}. Supported: {string.Join(", ", validLanguages)}");

        return errors.Any()
            ? ValidationResult.Fail(errors)
            : ValidationResult.Success();
    }

    // Serialize to JSON
    public string ToJson()
    {
        return _settings.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public override string ToString() => $"ConfigurationSettings (Count: {_settings.Count})";
}