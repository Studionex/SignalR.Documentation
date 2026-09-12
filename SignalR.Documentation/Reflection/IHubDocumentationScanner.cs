using System.Reflection;
using SignalR.Documentation.Models;

namespace SignalR.Documentation.Reflection;

public interface IHubDocumentationScanner
{
    IReadOnlyList<HubDocumentation> Scan(params Assembly[] assemblies);
}