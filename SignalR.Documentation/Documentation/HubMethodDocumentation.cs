namespace SignalR.Documentation.Models;

public sealed class HubMethodDocumentation
{
    public string MethodName { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string DisplaySignature { get; init; } = string.Empty;
    public bool IsAsync { get; init; }
    public ReturnDocumentation ReturnType { get; init; } = new();
    public List<HubParameterDocumentation> Parameters { get; init; } = [];
}
