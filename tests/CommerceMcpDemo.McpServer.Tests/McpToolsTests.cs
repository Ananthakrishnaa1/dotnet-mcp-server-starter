using System.Text.Json;
using CommerceMcpDemo.Application;
using CommerceMcpDemo.Infrastructure;
using CommerceMcpDemo.McpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CommerceMcpDemo.McpServer.Tests;

/// <summary>Verifies JSON-configured MCP tool discovery, dispatch, safe errors, and stdout hygiene.</summary>
public sealed class McpToolsTests
{
    /// <summary>Verifies tools.json exposes exactly the six intended read-only tools.</summary>
    [Fact]
    public void JsonConfigurationExposesOnlyExpectedReadTools()
    {
        var names = CreateCatalog().GetEnabledTools().Select(tool => tool.Name).OrderBy(name => name).ToArray();

        Assert.Equal<string>(["commerce_get_customer", "commerce_get_order", "commerce_get_product", "commerce_search_customers", "commerce_search_orders", "commerce_search_products"], names);
        Assert.DoesNotContain(names, name => name.Contains("create", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Verifies a configured tool invokes the same application service used by another adapter in one host.</summary>
    [Fact]
    public async Task ToolReadsDataFromSameInMemorySingletonAsApplicationService()
    {
        await using var provider = CreateProvider();
        var customers = provider.GetRequiredService<ICustomerService>();
        var created = await customers.CreateAsync(new CreateCustomerRequest("Shared Instance", "shared@example.test"), CancellationToken.None);

        var result = await CreateTools().InvokeAsync(
            "commerce_get_customer",
            new Dictionary<string, JsonElement> { ["customerId"] = JsonSerializer.SerializeToElement(created.Id) },
            provider,
            CancellationToken.None);

        Assert.False(result.IsError ?? false);
        Assert.Equal(created.Email, result.StructuredContent!.Value.GetProperty("email").GetString());
        Assert.Same(provider.GetRequiredService<HardcodedCommerceDataStore>(), provider.GetRequiredService<HardcodedCommerceDataStore>());
    }

    /// <summary>Verifies a successful configured tool invocation returns a deterministic product.</summary>
    [Fact]
    public async Task GetProductToolReturnsSeededProduct()
    {
        await using var provider = CreateProvider();

        var result = await CreateTools().InvokeAsync(
            "commerce_get_product",
            new Dictionary<string, JsonElement> { ["productId"] = JsonSerializer.SerializeToElement(Guid.Parse("10000000-0000-0000-0000-000000000001")) },
            provider,
            CancellationToken.None);

        Assert.False(result.IsError ?? false);
        Assert.Equal("Aurora Lamp", result.StructuredContent!.Value.GetProperty("name").GetString());
    }

    /// <summary>Verifies invalid inputs are converted to a safe MCP-facing result.</summary>
    [Fact]
    public async Task SearchToolMapsValidationFailuresToSafeResult()
    {
        await using var provider = CreateProvider();

        var result = await CreateTools().InvokeAsync(
            "commerce_search_products",
            new Dictionary<string, JsonElement> { ["pageSize"] = JsonSerializer.SerializeToElement(101) },
            provider,
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("Page size must be between 1 and 100.", GetText(result));
    }

    /// <summary>Verifies unexpected internal exception text is never returned by a configured tool.</summary>
    [Fact]
    public async Task ToolSanitizesUnexpectedExceptionText()
    {
        await using var provider = new ServiceCollection().AddSingleton<ICustomerService>(new ThrowingCustomerService()).BuildServiceProvider();

        var result = await CreateTools().InvokeAsync(
            "commerce_get_customer",
            new Dictionary<string, JsonElement> { ["customerId"] = JsonSerializer.SerializeToElement(Guid.NewGuid()) },
            provider,
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("The commerce tool could not complete the request.", GetText(result));
        Assert.DoesNotContain("secret", GetText(result), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies a direct configured tool invocation does not write log data to stdout.</summary>
    [Fact]
    public async Task ToolInvocationDoesNotWriteToStdout()
    {
        await using var provider = CreateProvider();
        var writer = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(writer);
            await CreateTools().InvokeAsync(
                "commerce_get_product",
                new Dictionary<string, JsonElement> { ["productId"] = JsonSerializer.SerializeToElement(Guid.Parse("10000000-0000-0000-0000-000000000001")) },
                provider,
                CancellationToken.None);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Equal(string.Empty, writer.ToString());
    }

    private static CommerceToolCatalog CreateCatalog() => new(Path.Combine(AppContext.BaseDirectory, "tools.json"));

    private static CommerceTools CreateTools() => new(CreateCatalog(), NullLogger<CommerceTools>.Instance);

    private static string GetText(CallToolResult result) => Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;

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
