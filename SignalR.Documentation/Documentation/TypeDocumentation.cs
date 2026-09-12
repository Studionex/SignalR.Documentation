namespace SignalR.Documentation.Models;

/// <summary>A type usage; object definitions are referenced by ID to support recursive models.</summary>
public sealed class TypeDocumentation
{
    public string TypeName { get; init; } = string.Empty;
    public string Kind { get; init; } = "scalar";
    public bool IsNullable { get; init; }
    public string? ModelId { get; init; }
    public TypeDocumentation? Items { get; init; }
    public TypeDocumentation? Keys { get; init; }
    public TypeDocumentation? Values { get; init; }
    public IReadOnlyList<string> EnumValues { get; init; } = [];
}

public sealed class ModelDocumentation
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<PropertyDocumentation> Properties { get; init; } = [];
}

public sealed class PropertyDocumentation
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public TypeDocumentation Type { get; init; } = new();
}
