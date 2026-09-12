namespace SignalR.Documentation.Reflection;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HubDocAttribute : Attribute
{
    public string? Summary { get; init; }
    public string? Description { get; init; }
}