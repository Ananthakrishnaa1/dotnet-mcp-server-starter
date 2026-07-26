using System.Reflection;
using CommerceMcpDemo.Application;
using CommerceMcpDemo.Infrastructure;
using CommerceMcpDemo.McpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CommerceMcpDemo.McpServer.Tests;

/// <summary>Verifies MCP tool discovery, invocation, safe errors, and stdout hygiene.</summary>
public sealed class McpToolsTests
{
    /// <summary>Verifies exactly the six read-only tool names are marked for MCP assembly discovery.</summary>
    [Fact]
    public void ToolAttributesExposeOnlyExpectedReadTools()
    {
        var names = typeof(CustomerTools).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>())
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!.Name ?? string.Empty)
            .OrderBy(name => name)
            .ToArray();
        Assert.Equal<string>(["commerce_get_customer", "commerce_get_order", "commerce_get_product", "commerce_search_customers", "commerce_search_orders", "commerce_search_products"], names);
        Assert.DoesNotContain(names, name => name.Contains("create", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies a tool invokes the same application service used by another adapter in one host.</summary>
    [Fact]
    public async Task ToolReadsDataFromSameInMemorySingletonAsApplicationService()
    {
        await using var provider = CreateProvider();
        var customers = provider.GetRequiredService<ICustomerService>();
        var created = await customers.CreateAsync(new CreateCustomerRequest("Shared Instance", "shared@example.test"), CancellationToken.None);
        var tools = new CustomerTools(customers);
        var result = await tools.GetCustomerAsync(created.Id, CancellationToken.None);
        Assert.Equal(created.Email, result.Email);
        Assert.Same(provider.GetRequiredService<HardcodedCommerceDataStore>(), provider.GetRequiredService<HardcodedCommerceDataStore>());
    }

    /// <summary>Verifies a successful tool invocation returns a deterministic product.</summary>
    [Fact]
    public async Task GetProductToolReturnsSeededProduct()
    {
        await using var provider = CreateProvider();
        var tools = new ProductTools(provider.GetRequiredService<IProductService>());
        var product = await tools.GetProductAsync(Guid.Parse("10000000-0000-0000-0000-000000000001"), CancellationToken.None);
        Assert.Equal("Aurora Lamp", product.Name);
    }

    /// <summary>Verifies invalid inputs are converted to a safe MCP-facing exception.</summary>
    [Fact]
    public async Task SearchToolMapsValidationFailuresToSafeException()
    {
        await using var provider = CreateProvider();
        var tools = new ProductTools(provider.GetRequiredService<IProductService>());
        var exception = await Assert.ThrowsAsync<SafeToolException>(() => tools.SearchProductsAsync(pageSize: 101));
        Assert.Equal("Page size must be between 1 and 100.", exception.Message);
    }

    /// <summary>Verifies unexpected internal exception text is never returned by a tool.</summary>
    [Fact]
    public async Task ToolSanitizesUnexpectedExceptionText()
    {
        var tools = new CustomerTools(new ThrowingCustomerService());
        var exception = await Assert.ThrowsAsync<SafeToolException>(() => tools.GetCustomerAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal("The commerce tool could not complete the request.", exception.Message);
        Assert.DoesNotContain("secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies a direct tool invocation does not write log data to stdout.</summary>
    [Fact]
    public async Task ToolInvocationDoesNotWriteToStdout()
    {
        await using var provider = CreateProvider();
        var writer = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(writer);
            var tools = new ProductTools(provider.GetRequiredService<IProductService>());
            await tools.GetProductAsync(Guid.Parse("10000000-0000-0000-0000-000000000001"), CancellationToken.None);
        }
        finally
        {
            Console.SetOut(original);
        }
        Assert.Equal(string.Empty, writer.ToString());
    }

    /// <summary>Builds one MCP-ready service provider with its shared in-memory singleton.</summary>
    private static ServiceProvider CreateProvider() => new ServiceCollection().AddCommerceApplication().AddCommerceInMemoryData().BuildServiceProvider();

    /// <summary>Supplies an unexpected failure for the tool exception-sanitization test.</summary>
    private sealed class ThrowingCustomerService : ICustomerService
    {
        /// <inheritdoc />
        public Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new InvalidOperationException("secret implementation detail");
        /// <inheritdoc />
        public Task<PagedResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken) => throw new InvalidOperationException("secret implementation detail");
        /// <inheritdoc />
        public Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken) => throw new InvalidOperationException("secret implementation detail");
    }
}
