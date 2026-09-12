namespace SignalR.Documentation.Models;

public sealed class ReturnDocumentation
{
    public string TypeName { get; init; } = string.Empty;
    public string FullTypeName { get; init; } = string.Empty;
    public bool HasReturnValue { get; init; }
    public bool IsAsyncWrapper { get; init; }
    public string? UnwrappedTypeName { get; init; }
    public string? UnwrappedFullTypeName { get; init; }
    public TypeDocumentation? Schema { get; init; }
}