using CommerceMcpDemo.Application;
using Microsoft.AspNetCore.Mvc;

namespace CommerceMcpDemo.Api.Controllers;

/// <summary>Exposes order operations over HTTP while delegating all behavior to application services.</summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Gets an order and its line items by identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Searches orders with optional customer, status, and date filters.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderDto>>> SearchAsync([FromQuery] OrderSearchRequest request, CancellationToken cancellationToken) => Ok(await orders.SearchAsync(request, cancellationToken));

    /// <summary>Creates a draft order in the current process's transient in-memory store.</summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDto>> CreateDraftAsync([FromBody] CreateDraftOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.CreateDraftAsync(request, cancellationToken);
        return Created($"/api/orders/{order.Id}", order);
    }
}
