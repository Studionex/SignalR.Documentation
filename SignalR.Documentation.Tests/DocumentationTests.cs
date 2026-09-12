using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using SignalR.Documentation.Models;
using SignalR.Documentation.Reflection;

namespace SignalR.Documentation.Tests;

public class DocumentationTests
{
    private static IReadOnlyList<HubDocumentation> Scan() => new HubDocumentationScanner().Scan(typeof(SampleHub).Assembly);
    private static HubDocumentation Sample() => Scan().Single(h => h.HubName == nameof(SampleHub));
    [Fact]
    public void FindsConcreteHubsOnceAndExcludesFrameworkMethods()
    {
        var scanner = new HubDocumentationScanner();
        var hubs = scanner.Scan(typeof(SampleHub).Assembly, typeof(SampleHub).Assembly);
        Assert.Single(hubs, h => h.HubName == nameof(SampleHub));
        Assert.DoesNotContain(hubs, h => h.HubName.Contains("Generic") || h.HubName == nameof(ParentHub));
        Assert.DoesNotContain(Sample().Methods, m => m.MethodName is "Dispose" or "OnConnectedAsync" or "GetHashCode" or "get_Context");
        Assert.Contains(Sample().Methods, m => m.MethodName == "Inherited");
        Assert.Contains(Sample().Methods, m => m.MethodName == "renamed");
        Assert.Empty(scanner.Scan());
    }
    [Fact]
    public void ComplexModelsContainNestedPropertiesAndCyclesTerminate()
    {
        var hub = Sample();
        var root = hub.Models.Single(m => m.Name == nameof(Payload));
        var child = hub.Models.Single(m => m.Name == nameof(Child));
        Assert.Equal(child.Id, root.Properties.Single(p => p.Name == "child").Type.ModelId);
        Assert.Equal(root.Id, root.Properties.Single(p => p.Name == "parent").Type.ModelId);
        Assert.DoesNotContain(root.Properties, p => p.Name is "hidden" or "staticValue" or "item" or "writeOnly");
        Assert.Contains(root.Properties, p => p.Name == "external_name" && p.IsRequired);
        Assert.Equal("A child.", child.Description);
        Assert.Contains(child.Properties, p => p.Name == "inheritedProperty");
    }
    [Fact]
    public void ScalarsAndEnumsDoNotBecomeComplexModels()
    {
        var hub = Sample();
        Assert.DoesNotContain(hub.Models, m => m.Name is "string" or "int" or "decimal" or "Guid" or "DateTime" or "State" or "DateOnly");
        var model = hub.Models.Single(m => m.Name == nameof(Payload));
        var state = model.Properties.Single(p => p.Name == "state").Type;
        Assert.Equal("enum", state.Kind);
        Assert.Equal("State", state.TypeName);
        Assert.Equal(new[] { "New", "Done" }, state.EnumValues);
        Assert.Null(state.ModelId);
        Assert.True(model.Properties.Single(p => p.Name == "optionalStruct").Type.IsNullable);
        Assert.NotNull(model.Properties.Single(p => p.Name == "optionalStruct").Type.ModelId);
    }
    [Fact]
    public void CollectionsAndDictionariesReferenceTheirElementSchemas()
    {
        var model = Sample().Models.Single(m => m.Name == nameof(Payload));
        var list = model.Properties.Single(p => p.Name == "children").Type;
        Assert.Equal("array", list.Kind);
        Assert.NotNull(list.Items!.ModelId);
        Assert.True(list.Items.IsNullable);
        var map = model.Properties.Single(p => p.Name == "lookup").Type;
        Assert.Equal("dictionary", map.Kind);
        Assert.Equal("string", map.Keys!.TypeName);
        Assert.Equal(list.Items.ModelId, map.Values!.ModelId);
        Assert.True(map.Values.IsNullable);
        Assert.Equal(list.Items.ModelId, model.Properties.Single(p => p.Name == "array").Type.Items!.ModelId);
    }
    [Fact]
    public void ParametersPreserveNullabilityDefaultsAndExcludeServices()
    {
        var method = Sample().Methods.Single(m => m.MethodName == "Send");
        Assert.Equal(3, method.Parameters.Count);
        Assert.True(method.Parameters[0].IsNullable);
        Assert.True(method.Parameters[1].IsOptional);
        Assert.Equal(10, method.Parameters[1].DefaultValue);
        Assert.True(method.Parameters[2].IsNullable);
        Assert.Equal("hello", method.Parameters[2].DefaultValue);
    }
    [Fact]
    public void UnwrapsTaskAndValueTaskAndIncludesReturnOnlyModels()
    {
        var hub = Sample();
        Assert.Contains(hub.Models, m => m.Name == nameof(ReturnOnly));
        Assert.True(hub.Methods.Single(m => m.MethodName == "Send").ReturnType.Schema!.IsNullable);
        Assert.Equal("array", hub.Methods.Single(m => m.MethodName == "Many").ReturnType.Schema!.Kind);
        foreach (var name in new[] { "NoResult", "NoValueTaskResult", "SyncVoid" })
        {
            Assert.Null(hub.Methods.Single(m => m.MethodName == name).ReturnType.Schema);
            Assert.False(hub.Methods.Single(m => m.MethodName == name).ReturnType.HasReturnValue);
        }
        Assert.Equal("string", hub.Methods.Single(m => m.MethodName == "renamed").ReturnType.Schema!.TypeName);
    }
    [Fact]
    public void RendererDisplaysSchemasModelsAndEncodesUntrustedText()
    {
        var html = HubDocumentationHtmlView.RenderPage(Scan(), "<script>alert('x')</script>");
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("external_name", html);
        Assert.Contains("Dictionary values", html);
        Assert.Contains("Recursive reference", html);
        Assert.Contains("Allowed:", html);
        Assert.Contains("id=\"models\"", html);
        Assert.True(html.IndexOf("id=\"models\"", StringComparison.Ordinal) > html.IndexOf("class=\"hub\"", StringComparison.Ordinal));
        foreach (var model in Scan().SelectMany(h => h.Models).DistinctBy(m => m.Id))
            Assert.Contains($"id=\"{model.Id}\"", html);
        Assert.Contains("No hubs found", HubDocumentationHtmlView.RenderPage([]));
        Assert.True(html.Length < 200_000);
    }
    [Fact]
    public void ScanStateIsIsolatedAcrossParallelCalls()
    {
        var scanner = new HubDocumentationScanner();
        Parallel.For(0, 12, _ => Assert.Equal(Sample().Models.Count,
            scanner.Scan(typeof(SampleHub).Assembly).Single(h => h.HubName == nameof(SampleHub)).Models.Count));
    }
}
public abstract class ParentHub : Hub { public string Inherited() => "ok"; }
public class GenericHub<T> : Hub { }
public class SampleHub : ParentHub
{
    public Task<ReturnOnly?> Send(Payload? input, int limit = 10, string? message = "hello", CancellationToken cancellationToken = default, [FromServices] IServiceProvider? services = null) => Task.FromResult<ReturnOnly?>(null);
    public ValueTask<Child[]> Many() => ValueTask.FromResult(Array.Empty<Child>());
    public Task NoResult() => Task.CompletedTask;
    public ValueTask NoValueTaskResult() => ValueTask.CompletedTask;
    public void SyncVoid() { }
    [HubMethodName("renamed")] public string Original() => "hello";
    public override Task OnConnectedAsync() => Task.CompletedTask;
}
public sealed class Payload
{
    [JsonPropertyName("external_name")] public required string Name { get; init; }
    public Child Child { get; init; } = new();
    public Payload? Parent { get; init; }
    public List<Child?> Children { get; init; } = [];
    public Child[] Array { get; init; } = [];
    public Dictionary<string, Child?> Lookup { get; init; } = [];
    public State State { get; init; }
    public decimal Amount { get; init; }
    public Guid Id { get; init; }
    public DateTime Date { get; init; }
    public DateOnly Day { get; init; }
    public Coordinates? OptionalStruct { get; init; }
    [JsonIgnore] public string Hidden { get; init; } = "";
    public static int StaticValue => 1;
    public string this[int i] => "";
    public string WriteOnly { private get; set; } = "";
}
public class BaseChild { public int InheritedProperty { get; init; } }
[Description("A child.")]
public sealed class Child : BaseChild { public string? Label { get; init; } }
public sealed record ReturnOnly(bool Success);
public readonly record struct Coordinates(double Latitude, double Longitude);
public enum State { New, Done }
