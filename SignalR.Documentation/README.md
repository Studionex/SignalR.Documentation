# SignalR.Documentation

Reflection-based SignalR documentation for ASP.NET Core on .NET 10.

Discover hub methods and display their parameters, return values, nested object properties, and shared model definitions in a self-contained, searchable HTML page.

## Installation

```shell
dotnet add package SignalR.Documentation --version 1.0.0
```

## Quick start

```csharp
using Microsoft.AspNetCore.SignalR;
using SignalR.Documentation;
using SignalR.Documentation.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSignalRDocumentation();

var app = builder.Build();
app.MapHub<OrdersHub>("/hubs/orders");
app.MapSignalRDocumentation("/realtime-docs", typeof(OrdersHub).Assembly);
app.Run();

[HubDoc(Summary = "Orders")]
public sealed class OrdersHub : Hub
{
    [HubMethodDoc(Summary = "Echo an order")]
    public Task<OrderRequest> CreateOrder(OrderRequest request) =>
        Task.FromResult(request);
}

public sealed record OrderRequest(string Customer, Address DeliveryAddress);
public sealed record Address(string Street, string City);
```

Open `/realtime-docs`. Expand a method, then **Properties** to inspect nested objects. **View model** opens the corresponding definition in the **Models** section.

The mapped documentation endpoint supports `RequireAuthorization()` and `AllowAnonymous()`. It does not automatically inherit authorization from the documented hub.

## Features

- Assembly scanning with `HubDoc` and `HubMethodDoc` attributes.
- Nested object properties and a deduplicated Models section.
- Arrays, collections, dictionaries, nullable types and recursive model references.
- Task and ValueTask return type unwrapping.
- Enum member names shown as allowed values, without treating enums as complex models.
- JSON property names, ignored properties, descriptions and required-member metadata.
- Inherited hub methods and `HubMethodName` aliases.
- Search, collapsible schemas and responsive layout; no CDN dependencies.

## Direct rendering

```csharp
using SignalR.Documentation.Models;
using SignalR.Documentation.Reflection;

var hubs = new HubDocumentationScanner().Scan(typeof(OrdersHub).Assembly);
var html = HubDocumentationHtmlView.RenderPage(hubs, "Orders API");
```

## Scope

Requires .NET 10 and the ASP.NET Core shared framework.

Property naming follows default SignalR JSON camelCase conventions and `JsonPropertyName`. Custom serializers, converters, naming policies, polymorphic discriminators, JSON fields and implicit service-parameter inference are not inferred. Enum names describe CLR values; the wire format depends on your serializer. This is a documentation viewer, not an OpenAPI generator or an interactive SignalR client.
