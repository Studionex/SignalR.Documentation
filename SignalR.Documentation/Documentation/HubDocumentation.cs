namespace SignalR.Documentation.Models;

public sealed class HubDocumentation
{
    public string HubName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Namespace { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public List<HubMethodDocumentation> Methods { get; init; } = [];
    public List<ModelDocumentation> Models { get; init; } = [];
}