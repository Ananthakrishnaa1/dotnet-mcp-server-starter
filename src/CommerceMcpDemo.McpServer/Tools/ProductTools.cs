using System.ComponentModel;
using CommerceMcpDemo.Application;
using ModelContextProtocol.Server;

namespace CommerceMcpDemo.McpServer.Tools;

/// <summary>Provides read-only product MCP tools backed by the application service layer.</summary>
[McpServerToolType]
public sealed class ProductTools(IProductService products)
{
    /// <summary>Gets one product by its unique identifier.</summary>
    [McpServerTool(Name = "commerce_get_product")]
    [Description("Gets one product by its unique identifier.")]
    public Task<ProductDto> GetProductAsync([Description("The unique product identifier.")] Guid productId, CancellationToken cancellationToken) =>
        McpToolGuard.ExecuteAsync(async () => await products.GetByIdAsync(productId, cancellationToken) ?? throw SafeToolException.NotFound("Product"));

    /// <summary>Searches products using bounded pagination.</summary>
    [McpServerTool(Name = "commerce_search_products")]
    [Description("Searches products by name or SKU with active, stock, and price filters.")]
    public Task<PagedResult<ProductDto>> SearchProductsAsync(
        [Description("Optional text contained in the product name or SKU.")] string? query = null,
        [Description("Optional active-product filter. Defaults to true.")] bool? isActive = true,
        [Description("Optional in-stock filter.")] bool? inStock = null,
        [Description("Optional inclusive maximum product price.")] decimal? maxPrice = null,
        [Description("One-based page number. Must be at least 1.")] int page = 1,
        [Description("Results per page. Must be between 1 and 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        McpToolGuard.ExecuteAsync(() => products.SearchAsync(new ProductSearchRequest(query, isActive, inStock, maxPrice, page, pageSize), cancellationToken));
}
