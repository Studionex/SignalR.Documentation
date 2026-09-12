namespace SignalR.Documentation.Reflection;

[AttributeUsage(AttributeTargets.Method)]
public sealed class HubMethodDocAttribute : Attribute
{
    public string? Summary { get; init; }

}