namespace SignalR.Documentation.Models;

public sealed class HubParameterDocumentation
{
    public string Name { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string FullTypeName { get; init; } = string.Empty;
    public bool IsNullable { get; init; }
    public bool IsOptional { get; init; }
    public object? DefaultValue { get; init; }
    public TypeDocumentation Schema { get; init; } = new();
}