using System.Net;
using System.Text;
using System.Text.Json;

namespace SignalR.Documentation.Models.View;

public static class HubDocumentationViewRenderer
{
    public const string DefaultTitle = "Realtime API";

    public static string RenderPage(IReadOnlyList<HubDocumentation> hubs, string title = DefaultTitle)
    {
        ArgumentNullException.ThrowIfNull(hubs);
        var models = hubs.SelectMany(h => h.Models).DistinctBy(m => m.Id).OrderBy(m => m.Name, StringComparer.Ordinal).ToDictionary(m => m.Id);
        var sb = new StringBuilder();
        sb.Append($"<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{H(title)}</title><style>{SocketDocsCss.Content}</style></head><body>");
        sb.Append("<aside><a class=\"brand\" href=\"#overview\"><span class=\"logo\">↔</span> Realtime<span class=\"muted\"> / docs</span></a><label class=\"search-label\" for=\"search\">Search documentation</label><input id=\"search\" type=\"search\" placeholder=\"Search methods, models…\"><nav aria-label=\"Documentation\"><span class=\"eyebrow\">HUBS</span>");
        for (var i = 0; i < hubs.Count; i++)
            sb.Append($"<a href=\"#hub-{i}\">{H(hubs[i].HubName)} <span class=\"count\">{hubs[i].Methods.Count}</span></a>");
        sb.Append($"<a href=\"#models\">Models <span class=\"count\">{models.Count}</span></a></nav><p class=\"aside-note\">SignalR reference<br>Methods, parameters &amp; schemas</p></aside><main id=\"overview\"><header><span class=\"eyebrow accent\">API REFERENCE</span><h1>{H(title)}</h1><p class=\"intro\">Explore hub methods and the objects they exchange.</p><div class=\"stats\">{hubs.Count} hubs <span>·</span> {hubs.Sum(h => h.Methods.Count)} methods <span>·</span> {models.Count} models</div></header><p id=\"no-results\" hidden role=\"status\">No matching methods or models.</p>");
        if (hubs.Count == 0) sb.Append("<p class=\"empty\">No hubs found in the supplied assemblies.</p>");
        for (var i = 0; i < hubs.Count; i++)
        {
            var hub = hubs[i];
            sb.Append($"<section class=\"hub\" id=\"hub-{i}\"><h2>{H(hub.HubName)}</h2><p>{H(hub.Summary)}</p>");
            if (!string.IsNullOrWhiteSpace(hub.Description)) sb.Append($"<p class=\"muted\">{H(hub.Description)}</p>");
            foreach (var method in hub.Methods)
            {
                sb.Append($"<details class=\"method searchable\" data-hub=\"{H(hub.HubName)}\"><summary><span class=\"method-kind\">CALL</span><strong>{H(method.MethodName)}</strong><span class=\"method-description\">{H(method.Summary)}</span><span class=\"chevron\">⌄</span></summary><div class=\"method-body\"><h3>Parameters <span class=\"count\">{method.Parameters.Count}</span></h3>");
                if (method.Parameters.Count == 0) sb.Append("<p class=\"muted\">No parameters.</p>");
                foreach (var parameter in method.Parameters)
                {
                    sb.Append($"<div class=\"field\"><div class=\"field-heading\"><code>{H(parameter.Name)}</code><span class=\"{(parameter.IsOptional ? "muted" : "required")}\">{(parameter.IsOptional ? "optional" : "required")}</span></div>");
                    RenderType(sb, parameter.Schema, models, []);
                    if (parameter.IsOptional) sb.Append($"<p class=\"muted\">Default: <code>{H(JsonSerializer.Serialize(parameter.DefaultValue))}</code></p>");
                    sb.Append("</div>");
                }
                sb.Append("<h3>Returns</h3><div class=\"field\">");
                if (method.ReturnType.Schema is { } schema) RenderType(sb, schema, models, []);
                else sb.Append("<span class=\"muted\">No return value</span>");
                sb.Append("</div></div></details>");
            }
            if (hub.Methods.Count == 0) sb.Append("<p class=\"empty\">No callable methods.</p>");
            sb.Append("</section>");
        }
        sb.Append($"<section id=\"models\"><div class=\"section-heading\"><h2>Models</h2><span class=\"count\">{models.Count}</span></div><p class=\"muted\">Object schemas used by method parameters and return values.</p>");
        foreach (var model in models.Values)
        {
            sb.Append($"<details id=\"{H(model.Id)}\" class=\"model searchable\"><summary><span class=\"object-icon\">{{ }}</span><strong>{H(model.Name)}</strong><span class=\"method-description\">{model.Properties.Count} properties</span><span class=\"chevron\">⌄</span></summary><div class=\"model-body\"><p class=\"qualified\">{H(model.FullName)}</p><p>{H(model.Description)}</p>");
            RenderProperties(sb, model, models, [model.Id]);
            sb.Append("</div></details>");
        }
        if (models.Count == 0) sb.Append("<p class=\"empty\">No complex models are used by these hubs.</p>");
        sb.Append("""
            </section><footer>SignalR documentation</footer></main><script>
            const search = document.getElementById('search');
            search.addEventListener('input', () => {
              const q = search.value.trim().toLowerCase();
              let matches = 0;
              document.querySelectorAll('.searchable').forEach(el => {
                el.hidden = !(el.textContent + ' ' + (el.dataset.hub || '')).toLowerCase().includes(q);
                if (!el.hidden) matches++;
              });
              document.querySelectorAll('.hub').forEach(el => {
                el.hidden = !!q && !Array.from(el.querySelectorAll('.searchable')).some(item => !item.hidden);
              });
              document.getElementById('no-results').hidden = !q || matches > 0;
            });
            function revealTarget() {
              const el = document.getElementById(location.hash.slice(1));
              if (!el) return;
              if (search.value) { search.value = ''; search.dispatchEvent(new Event('input')); }
              if (el.tagName === 'DETAILS') el.open = true;
              el.scrollIntoView({block:'start'});
            }
            window.addEventListener('hashchange', revealTarget);
            document.addEventListener('click', event => {
              const link = event.target.closest('a[href^="#"]');
              if (link && link.hash === location.hash) revealTarget();
            });
            if (location.hash) revealTarget();
            </script></body></html>
            """);
        return sb.ToString();
    }

    private static void RenderType(StringBuilder sb, TypeDocumentation type, IReadOnlyDictionary<string, ModelDocumentation> models, HashSet<string> ancestors)
    {
        sb.Append($"<div class=\"type-line\"><code class=\"type\">{H(type.TypeName)}</code>");
        if (type.IsNullable) sb.Append("<span class=\"nullable\">nullable</span>");
        if (type.ModelId is { } id) sb.Append($"<a class=\"model-link\" href=\"#{H(id)}\">View model ↗</a>");
        sb.Append("</div>");
        if (type.EnumValues.Count > 0)
            sb.Append($"<p class=\"enum-values\">Allowed: {string.Join(" · ", type.EnumValues.Select(v => $"<code>{H(v)}</code>"))}</p>");
        if (type.Items is { } items)
        {
            sb.Append("<details class=\"nested\" open><summary>Items</summary>");
            RenderType(sb, items, models, ancestors);
            sb.Append("</details>");
        }
        if (type.Values is { } values)
        {
            sb.Append($"<details class=\"nested\" open><summary>Dictionary values <span class=\"muted\">· keys: {H(type.Keys?.TypeName)}</span></summary>");
            RenderType(sb, values, models, ancestors);
            sb.Append("</details>");
        }
        if (type.ModelId is not { } modelId || !models.TryGetValue(modelId, out var model)) return;
        if (ancestors.Contains(modelId))
        {
            sb.Append("<p class=\"muted recursive\">Recursive reference — open the model to inspect its properties.</p>");
            return;
        }
        // Keep large graphs bounded; all definitions remain available in Models.
        if (ancestors.Count >= 6) return;
        sb.Append("<details class=\"nested\"><summary>Properties</summary>");
        RenderProperties(sb, model, models, new HashSet<string>(ancestors) { modelId });
        sb.Append("</details>");
    }

    private static void RenderProperties(StringBuilder sb, ModelDocumentation model, IReadOnlyDictionary<string, ModelDocumentation> models, HashSet<string> ancestors)
    {
        if (model.Properties.Count == 0) sb.Append("<p class=\"muted\">No public serializable properties.</p>");
        foreach (var property in model.Properties)
        {
            sb.Append($"<div class=\"field\"><div class=\"field-heading\"><code>{H(property.Name)}</code>");
            if (property.IsRequired) sb.Append("<span class=\"required\">required</span>");
            sb.Append("</div>");
            RenderType(sb, property.Type, models, ancestors);
            if (!string.IsNullOrWhiteSpace(property.Description)) sb.Append($"<p class=\"property-description\">{H(property.Description)}</p>");
            sb.Append("</div>");
        }
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
