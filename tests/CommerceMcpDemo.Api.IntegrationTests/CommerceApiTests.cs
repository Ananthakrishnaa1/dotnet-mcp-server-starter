using System.Net;
using System.Net.Http.Json;
using CommerceMcpDemo.Application;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CommerceMcpDemo.Api.IntegrationTests;

/// <summary>Verifies public HTTP routes and ProblemDetails behavior.</summary>
public sealed class CommerceApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();

    /// <summary>Verifies a seeded product can be fetched over its controller route.</summary>
    [Fact]
    public async Task GetProductReturnsSeededValue()
    {
        var response = await client.GetAsync("/api/products/10000000-0000-0000-0000-000000000001");
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Aurora Lamp", product?.Name);
    }

    /// <summary>Verifies absent resources follow the controller's 404 contract.</summary>
    [Fact]
    public async Task GetCustomerReturnsNotFoundWhenAbsent()
    {
        var response = await client.GetAsync("/api/customers/ffffffff-ffff-ffff-ffff-ffffffffffff");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies list routes return a validation ProblemDetails response for excessive page sizes.</summary>
    [Fact]
    public async Task SearchProductsReturnsBadRequestForExcessivePageSize()
    {
        var response = await client.GetAsync("/api/products?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Verifies a created customer is available from the same API host's transient singleton store.</summary>
    [Fact]
    public async Task CreateCustomerIsVisibleInSameApiHost()
    {
        var response = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest("Test Customer", "created@example.test"));
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var lookup = await client.GetAsync($"/api/customers/{customer?.Id}");
        Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
    }
}
