using CommerceMcpDemo.Application;
using Microsoft.AspNetCore.Mvc;

namespace CommerceMcpDemo.Api.Controllers;

/// <summary>Exposes customer operations over HTTP while delegating all behavior to application services.</summary>
[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ICustomerService customers) : ControllerBase
{
    /// <summary>Gets a customer by its stable identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>Searches customers with optional text and status filters.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<CustomerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> SearchAsync([FromQuery] CustomerSearchRequest request, CancellationToken cancellationToken) => Ok(await customers.SearchAsync(request, cancellationToken));

    /// <summary>Creates a customer in the current process's transient in-memory store.</summary>
    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDto>> CreateAsync([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customers.CreateAsync(request, cancellationToken);
        return Created($"/api/customers/{customer.Id}", customer);
    }
}
