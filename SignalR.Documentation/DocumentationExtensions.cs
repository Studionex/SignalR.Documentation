using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SignalR.Documentation.Models;
using SignalR.Documentation.Reflection;

namespace SignalR.Documentation;

public static class DocumentationExtensions
{
    public static IServiceCollection AddSignalRDocumentation(this IServiceCollection services)
    {
        services.TryAddSingleton<IHubDocumentationScanner, HubDocumentationScanner>();
        return services;
    }

    public static IEndpointConventionBuilder MapSignalRDocumentation(this IEndpointRouteBuilder endpoints,
        string pattern = "/realtime-docs", params Assembly[] assemblies)
    {
        return endpoints.MapGet(pattern, (IHubDocumentationScanner scanner) =>
            Results.Content(HubDocumentationHtmlView.RenderPage(scanner.Scan(assemblies)), "text/html; charset=utf-8"))
            .WithDisplayName("SignalR documentation");
    }
}
