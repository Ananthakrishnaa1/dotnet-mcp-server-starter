using System.ComponentModel;
using CommerceMcpDemo.Application;
using CommerceMcpDemo.Domain;
using ModelContextProtocol.Server;

namespace CommerceMcpDemo.McpServer.Tools;

/// <summary>Provides read-only customer MCP tools backed by the application service layer.</summary>
[McpServerToolType]
public sealed class CustomerTools(ICustomerService customers)
{
    /// <summary>Gets one customer by its unique identifier.</summary>
    [McpServerTool(Name = "commerce_get_customer")]
    [Description("Gets one customer by its unique identifier.")]
    public Task<CustomerDto> GetCustomerAsync([Description("The unique customer identifier.")] Guid customerId, CancellationToken cancellationToken) =>
        McpToolGuard.ExecuteAsync(async () => await customers.GetByIdAsync(customerId, cancellationToken) ?? throw SafeToolException.NotFound("Customer"));

    /// <summary>Searches customers using bounded pagination.</summary>
    [McpServerTool(Name = "commerce_search_customers")]
    [Description("Searches customers by name or email, optionally filtering by status.")]
    public Task<PagedResult<CustomerDto>> SearchCustomersAsync(
        [Description("Optional text contained in the customer name or email.")] string? query = null,
        [Description("Optional customer status filter.")] CustomerStatus? status = null,
        [Description("One-based page number. Must be at least 1.")] int page = 1,
        [Description("Results per page. Must be between 1 and 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        McpToolGuard.ExecuteAsync(() => customers.SearchAsync(new CustomerSearchRequest(query, status, page, pageSize), cancellationToken));
}
