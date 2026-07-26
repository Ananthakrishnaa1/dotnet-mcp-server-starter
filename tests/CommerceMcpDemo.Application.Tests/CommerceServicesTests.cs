using CommerceMcpDemo.Application;
using CommerceMcpDemo.Domain;
using CommerceMcpDemo.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceMcpDemo.Application.Tests;

/// <summary>Verifies business rules and deterministic repository behavior.</summary>
public sealed class CommerceServicesTests
{
    /// <summary>Verifies a duplicate email is rejected by the shared customer service.</summary>
    [Fact]
    public async Task CreateCustomerAsyncRejectsDuplicateEmail()
    {
        await using var provider = CreateProvider();
        var customers = provider.GetRequiredService<ICustomerService>();
        await Assert.ThrowsAsync<ConflictException>(() => customers.CreateAsync(new CreateCustomerRequest("Duplicate", "anika@example.test"), CancellationToken.None));
    }

    /// <summary>Verifies the deterministic repository exposes the expected seeded catalog.</summary>
    [Fact]
    public async Task ProductRepositoryReturnsHardcodedCatalog()
    {
        await using var provider = CreateProvider();
        var products = provider.GetRequiredService<IProductRepository>();
        var values = await products.GetAllAsync(CancellationToken.None);
        Assert.Equal(20, values.Count);
        Assert.Contains(values, product => product.Sku == "LMP-001");
    }

    /// <summary>Verifies services enforce the maximum supported page size.</summary>
    [Fact]
    public async Task SearchAsyncRejectsPageSizeAbove100()
    {
        await using var provider = CreateProvider();
        var products = provider.GetRequiredService<IProductService>();
        await Assert.ThrowsAsync<RequestValidationException>(() => products.SearchAsync(new ProductSearchRequest(PageSize: 101), CancellationToken.None));
    }

    /// <summary>Verifies cancellation is propagated into repository and service operations.</summary>
    [Fact]
    public async Task SearchAsyncHonorsCancelledToken()
    {
        await using var provider = CreateProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var customers = provider.GetRequiredService<ICustomerService>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => customers.SearchAsync(new CustomerSearchRequest(), cancellation.Token));
    }

    /// <summary>Builds one host service provider with its shared in-memory singleton.</summary>
    private static ServiceProvider CreateProvider() => new ServiceCollection().AddCommerceApplication().AddCommerceInMemoryData().BuildServiceProvider();
}
