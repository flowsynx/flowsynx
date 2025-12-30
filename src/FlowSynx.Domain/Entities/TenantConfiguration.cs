using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Entities;

public class TenantConfiguration : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string"; // string, int, bool, decimal, json

    public object GetTypedValue()
    {
        return ValueType.ToLower() switch
        {
            "int" => int.TryParse(Value, out int intVal) ? intVal : 0,
            "bool" => bool.TryParse(Value, out bool boolVal) && boolVal,
            "decimal" => decimal.TryParse(Value, out decimal decVal) ? decVal : 0m,
            "json" => string.IsNullOrWhiteSpace(Value)
                ? new object()
                : System.Text.Json.JsonSerializer.Deserialize<object>(Value) ?? new object(),
            _ => Value
        };
    }
}