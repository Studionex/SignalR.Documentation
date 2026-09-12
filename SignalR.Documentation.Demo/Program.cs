using System.ComponentModel;
using Microsoft.AspNetCore.SignalR;
using SignalR.Documentation;
using SignalR.Documentation.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSignalRDocumentation();
var app = builder.Build();
app.MapHub<OrdersHub>("/hubs/orders");
app.MapSignalRDocumentation("/realtime-docs", typeof(OrdersHub).Assembly);
app.MapGet("/", () => Results.Redirect("/realtime-docs"));
app.Run();

[HubDoc(Summary = "Orders and delivery updates", Description = "Create orders, inspect delivery details, and follow their status in real time.")]
public sealed class OrdersHub : Hub
{
    [HubMethodDoc(Summary = "Create an order with delivery details")]
    public Task<OrderReceipt> CreateOrder(CreateOrderRequest request) =>
        Task.FromResult(new OrderReceipt(Guid.NewGuid(), OrderStatus.Pending, request));
    [HubMethodDoc(Summary = "Retrieve orders by customer")]
    public ValueTask<List<OrderReceipt>> GetOrders(Guid customerId, int limit = 20) => ValueTask.FromResult(new List<OrderReceipt>());
    [HubMethodDoc(Summary = "Check the connection")]
    public string Ping() => "pong";
}
public sealed class CreateOrderRequest
{
    [Description("The customer placing this order.")]
    public required string CustomerName { get; init; }
    public required Address DeliveryAddress { get; init; }
    public required List<OrderItem> Items { get; init; }
    public Dictionary<string, string?> Metadata { get; init; } = [];
    public string? Notes { get; init; }
}
public sealed record Address(string Street, string City, string? Apartment);
public sealed record OrderItem(string ProductName, int Quantity, decimal Price);
public sealed record OrderReceipt(Guid Id, OrderStatus Status, CreateOrderRequest Order);
public enum OrderStatus { Pending, Preparing, Delivered }
