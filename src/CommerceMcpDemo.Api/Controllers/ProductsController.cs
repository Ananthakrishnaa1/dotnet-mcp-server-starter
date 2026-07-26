using CommerceMcpDemo.Application;
using Microsoft.AspNetCore.Mvc;

namespace CommerceMcpDemo.Api.Controllers;

/// <summary>Exposes product read operations over HTTP.</summary>
[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    /// <summary>Gets a product by its stable identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Searches products with optional catalog filters.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> SearchAsync([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken) => Ok(await products.SearchAsync(request, cancellationToken));
}
