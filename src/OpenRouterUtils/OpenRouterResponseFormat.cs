namespace OpenRouterUtils;

/// <summary>
/// Describes a requested response format without requiring reflection-based typed deserialization.
/// </summary>
public sealed class OpenRouterResponseFormat
{
    private OpenRouterResponseFormat(string type, string? schemaName, string? schemaJson, bool strict)
    {
        Type = type;
        SchemaName = schemaName;
        SchemaJson = schemaJson;
        Strict = strict;
    }

    public string Type { get; }

    public string? SchemaName { get; }

    public string? SchemaJson { get; }

    public bool Strict { get; }

    public static OpenRouterResponseFormat JsonObject()
    {
        return new OpenRouterResponseFormat("json_object", null, null, false);
    }

    public static OpenRouterResponseFormat JsonSchema(string name, string schemaJson, bool strict = true)
    {
        return new OpenRouterResponseFormat("json_schema", name, schemaJson, strict);
    }
}
