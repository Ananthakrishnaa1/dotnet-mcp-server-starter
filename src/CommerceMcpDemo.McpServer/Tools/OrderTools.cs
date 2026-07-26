using System.ComponentModel;
using CommerceMcpDemo.Application;
using CommerceMcpDemo.Domain;
using ModelContextProtocol.Server;

namespace CommerceMcpDemo.McpServer.Tools;

/// <summary>Provides read-only order MCP tools backed by the application service layer.</summary>
[McpServerToolType]
public sealed class OrderTools(IOrderService orders)
{
    /// <summary>Gets one order and its items by its unique identifier.</summary>
    [McpServerTool(Name = "commerce_get_order")]
    [Description("Gets one order with its items by its unique identifier.")]
    public Task<OrderDto> GetOrderAsync([Description("The unique order identifier.")] Guid orderId, CancellationToken cancellationToken) =>
        McpToolGuard.ExecuteAsync(async () => await orders.GetByIdAsync(orderId, cancellationToken) ?? throw SafeToolException.NotFound("Order"));

    /// <summary>Searches orders using bounded pagination.</summary>
    [McpServerTool(Name = "commerce_search_orders")]
    [Description("Searches orders by customer, status, and creation time.")]
    public Task<PagedResult<OrderDto>> SearchOrdersAsync(
        [Description("Optional customer identifier filter.")] Guid? customerId = null,
        [Description("Optional order status filter.")] OrderStatus? status = null,
        [Description("Optional inclusive UTC creation timestamp lower bound.")] DateTime? createdAfterUtc = null,
        [Description("One-based page number. Must be at least 1.")] int page = 1,
        [Description("Results per page. Must be between 1 and 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        McpToolGuard.ExecuteAsync(() => orders.SearchAsync(new OrderSearchRequest(customerId, status, createdAfterUtc, page, pageSize), cancellationToken));
}
