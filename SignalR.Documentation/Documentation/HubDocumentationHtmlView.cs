using SignalR.Documentation.Models.View;

namespace SignalR.Documentation.Models;

public static class HubDocumentationHtmlView
{
    public static string RenderPage(
        IReadOnlyList<HubDocumentation> hubs,
        string title = HubDocumentationViewRenderer.DefaultTitle)
        => HubDocumentationViewRenderer.RenderPage(hubs, title);
}
