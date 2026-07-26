using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommerceMcpDemo.Application;
using CommerceMcpDemo.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CommerceMcpDemo.McpServer.Tools;

/// <summary>
/// Lists JSON-configured commerce tools and dispatches their allowlisted operations to application services.
/// </summary>
public sealed class CommerceTools(CommerceToolCatalog catalog, ILogger<CommerceTools> logger)
{
    private static readonly JsonSerializerOptions ResultJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly Action<ILogger, Exception?> LogToolConfigurationFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1, nameof(LogToolConfigurationFailure)),
        "Unable to load the MCP tool configuration.");
    private static readonly Action<ILogger, string, Exception?> LogToolFailure = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(2, nameof(LogToolFailure)),
        "MCP tool {ToolName} failed.");

    /// <summary>Builds the MCP tool list from the current JSON configuration.</summary>
    public ValueTask<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return ValueTask.FromResult(new ListToolsResult { Tools = catalog.GetEnabledTools().Select(tool => tool.ToProtocolTool()).ToArray() });
        }
        catch (Exception exception)
        {
            LogToolConfigurationFailure(logger, exception);
            return ValueTask.FromResult(new ListToolsResult());
        }
    }

    /// <summary>Executes a JSON-configured tool request from the MCP server pipeline.</summary>
    public ValueTask<CallToolResult> CallToolAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken) =>
        new(InvokeAsync(request.Params.Name, request.Params.Arguments, request.Services ?? throw new InvalidOperationException("MCP request services are unavailable."), cancellationToken));

    /// <summary>Executes an allowlisted operation for a configured tool.</summary>
    public async Task<CallToolResult> InvokeAsync(
        string toolName,
        IDictionary<string, JsonElement>? arguments,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        try
        {
            var definition = catalog.GetEnabledTool(toolName);
            if (definition is null)
            {
                return Failure("The requested tool is not available.");
            }

            object value = definition.Operation switch
            {
                CommerceToolOperations.GetCustomer => await GetCustomerAsync(arguments, services, cancellationToken),
                CommerceToolOperations.SearchCustomers => await SearchCustomersAsync(arguments, services, cancellationToken),
                CommerceToolOperations.GetProduct => await GetProductAsync(arguments, services, cancellationToken),
                CommerceToolOperations.SearchProducts => await SearchProductsAsync(arguments, services, cancellationToken),
                CommerceToolOperations.GetOrder => await GetOrderAsync(arguments, services, cancellationToken),
                CommerceToolOperations.SearchOrders => await SearchOrdersAsync(arguments, services, cancellationToken),
                _ => throw new SafeToolException("The requested tool is not available.")
            };

            return Success(value);
        }
        catch (SafeToolException exception)
        {
            return Failure(exception.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogToolFailure(logger, toolName, exception);
            return Failure("The commerce tool could not complete the request.");
        }
    }

    private static async Task<CustomerDto> GetCustomerAsync(IDictionary<string, JsonElement>? arguments, IServiceProvider services, CancellationToken cancellationToken) =>
        await McpToolGuard.ExecuteAsync(async () =>
            await services.GetRequiredService<ICustomerService>().GetByIdAsync(GetRequiredGuid(arguments, "customerId"), cancellationToken)
            ?? throw SafeToolException.NotFound("Customer"));

    private static Task<PagedResult<CustomerDto>> SearchCustomersAsync(IDictionary<string, JsonElement>? arguments, IServiceProvider services, CancellationToken cancellationToken) =>
        McpToolGuard.ExecuteAsync(() => services.GetRequiredService<ICustomerService>().SearchAsync(
            new CustomerSearchRequest(
                GetOptionalString(arguments, "query"),
                GetOptionalEnum<CustomerStatus>(arguments, "status"),
                GetOptionalInt(arguments, "page", 1),
                GetOptionalInt(arguments, "pageSize", 20)),
            cancellationToken));

    private static async Task<ProductDto> GetProductAsync(IDictionary<string, JsonElement>? arguments, IServiceProvider services, CancellationToken cancellationToken) =>
        await McpToolGuard.ExecuteAsync(async () =>
            await services.GetRequiredService<IProductService>().GetByIdAsync(GetRequiredGuid(arguments, "productId"), cancellationToken)
            ?? throw SafeToolException.NotFound("Product"));

    private static Task<PagedResult<ProductDto>> SearchProductsAsync(IDictionary<string, JsonElement>? arguments, IServiceProvider services, CancellationToken cancellationToken) =>
        McpToolGuard.ExecuteAsync(() => services.GetRequiredService<IProductService>().SearchAsync(
            new ProductSearchRequest(
                GetOptionalString(arguments, "query"),
                GetOptionalBoolean(arguments, "isActive", true),
                GetOptionalBoolean(arguments, "inStock", null),
                GetOptionalDecimal(arguments, "maxPrice"),
                GetOptionalInt(arguments, "page", 1),
                GetOptionalInt(arguments, "pageSize", 20)),
            cancellationToken));

    private static async Task<OrderDto> GetOrderAsync(IDictionary<string, JsonElement>? arguments, IServiceProvider services, CancellationToken cancellationToken) =>
        await McpToolGuard.ExecuteAsync(async () =>
            await services.GetRequiredService<IOrderService>().GetByIdAsync(GetRequiredGuid(arguments, "orderId"), cancellationToken)
            ?? throw SafeToolException.NotFound("Order"));

    private static Task<PagedResult<OrderDto>> SearchOrdersAsync(IDictionary<string, JsonElement>? arguments, IServiceProvider services, CancellationToken cancellationToken) =>
        McpToolGuard.ExecuteAsync(() => services.GetRequiredService<IOrderService>().SearchAsync(
            new OrderSearchRequest(
                GetOptionalGuid(arguments, "customerId"),
                GetOptionalEnum<OrderStatus>(arguments, "status"),
                GetOptionalUtcDateTime(arguments, "createdAfterUtc"),
                GetOptionalInt(arguments, "page", 1),
                GetOptionalInt(arguments, "pageSize", 20)),
            cancellationToken));

    private static CallToolResult Success(object value)
    {
        var structuredContent = JsonSerializer.SerializeToElement(value, ResultJsonOptions);
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = structuredContent.GetRawText() }],
            StructuredContent = structuredContent
        };
    }

    private static CallToolResult Failure(string message) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = message }]
    };

    private static Guid GetRequiredGuid(IDictionary<string, JsonElement>? arguments, string name)
    {
        var value = GetRequiredValue(arguments, name);
        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var guid))
        {
            return guid;
        }

        throw new SafeToolException($"'{name}' must be a valid GUID.");
    }

    private static Guid? GetOptionalGuid(IDictionary<string, JsonElement>? arguments, string name)
    {
        if (!TryGetValue(arguments, name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var guid))
        {
            return guid;
        }

        throw new SafeToolException($"'{name}' must be a valid GUID.");
    }

    private static string? GetOptionalString(IDictionary<string, JsonElement>? arguments, string name)
    {
        if (!TryGetValue(arguments, name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        throw new SafeToolException($"'{name}' must be a string.");
    }

    private static TEnum? GetOptionalEnum<TEnum>(IDictionary<string, JsonElement>? arguments, string name)
        where TEnum : struct, Enum
    {
        if (!TryGetValue(arguments, name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String && Enum.TryParse<TEnum>(value.GetString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new SafeToolException($"'{name}' must be a valid {typeof(TEnum).Name} value.");
    }

    private static int GetOptionalInt(IDictionary<string, JsonElement>? arguments, string name, int defaultValue)
    {
        if (!TryGetValue(arguments, name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result))
        {
            return result;
        }

        throw new SafeToolException($"'{name}' must be an integer.");
    }

    private static bool? GetOptionalBoolean(IDictionary<string, JsonElement>? arguments, string name, bool? defaultValue)
    {
        if (!TryGetValue(arguments, name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        throw new SafeToolException($"'{name}' must be a boolean.");
    }

    private static decimal? GetOptionalDecimal(IDictionary<string, JsonElement>? arguments, string name)
    {
        if (!TryGetValue(arguments, name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var result))
        {
            return result;
        }

        throw new SafeToolException($"'{name}' must be a number.");
    }

    private static DateTime? GetOptionalUtcDateTime(IDictionary<string, JsonElement>? arguments, string name)
    {
        if (!TryGetValue(arguments, name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
        {
            return result.UtcDateTime;
        }

        throw new SafeToolException($"'{name}' must be an ISO-8601 UTC timestamp.");
    }

    private static JsonElement GetRequiredValue(IDictionary<string, JsonElement>? arguments, string name)
    {
        if (TryGetValue(arguments, name, out var value) && value.ValueKind != JsonValueKind.Null)
        {
            return value;
        }

        throw new SafeToolException($"'{name}' is required.");
    }

    private static bool TryGetValue(IDictionary<string, JsonElement>? arguments, string name, out JsonElement value)
    {
        value = default;
        return arguments is not null && arguments.TryGetValue(name, out value);
    }
}

/// <summary>Loads and validates the JSON tool catalogue used by <see cref="CommerceTools"/>.</summary>
public sealed class CommerceToolCatalog(string toolsFilePath)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Loads the enabled tool definitions from the JSON file.</summary>
    public IReadOnlyList<CommerceToolDefinition> GetEnabledTools() => Load().Where(tool => tool.Enabled).ToArray();

    /// <summary>Gets one enabled tool definition by its public MCP name.</summary>
    public CommerceToolDefinition? GetEnabledTool(string name) => Load().SingleOrDefault(tool => tool.Enabled && string.Equals(tool.Name, name, StringComparison.Ordinal));

    private List<CommerceToolDefinition> Load()
    {
        var contents = File.ReadAllText(toolsFilePath);
        var catalogue = JsonSerializer.Deserialize<CommerceToolCatalogueFile>(contents, Options)
            ?? throw new InvalidOperationException("The MCP tool configuration is empty.");
        var tools = catalogue.Tools ?? [];
        if (tools.Count == 0)
        {
            throw new InvalidOperationException("The MCP tool configuration contains no tools.");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tool in tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Name) || string.IsNullOrWhiteSpace(tool.Operation))
            {
                throw new InvalidOperationException("Each configured MCP tool requires a name and operation.");
            }

            if (!names.Add(tool.Name))
            {
                throw new InvalidOperationException($"The MCP tool configuration contains duplicate tool name '{tool.Name}'.");
            }

            if (!CommerceToolOperations.All.Contains(tool.Operation))
            {
                throw new InvalidOperationException($"The MCP tool operation '{tool.Operation}' is not allowlisted.");
            }

            _ = tool.ToProtocolTool();
        }

        return tools;
    }
}

/// <summary>Represents the root of tools.json.</summary>
public sealed class CommerceToolCatalogueFile
{
    /// <summary>Gets or sets the configured tool definitions.</summary>
    public List<CommerceToolDefinition>? Tools { get; init; }
}

/// <summary>Represents one MCP tool whose metadata is defined in tools.json.</summary>
public sealed class CommerceToolDefinition
{
    /// <summary>Gets or sets the MCP-visible tool name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets or sets the MCP-visible tool description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets or sets the fixed allowlisted operation to execute.</summary>
    public required string Operation { get; init; }
    /// <summary>Gets or sets whether the tool is advertised and callable.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Gets or sets the JSON Schema presented to MCP clients.</summary>
    public JsonElement InputSchema { get; init; }

    /// <summary>Converts this configuration record to an MCP protocol tool definition.</summary>
    public Tool ToProtocolTool() => new()
    {
        Name = Name,
        Description = Description,
        InputSchema = InputSchema
    };
}

/// <summary>Names the only application operations that JSON configuration may expose.</summary>
public static class CommerceToolOperations
{
    /// <summary>Gets one customer.</summary>
    public const string GetCustomer = "getCustomer";
    /// <summary>Searches customers.</summary>
    public const string SearchCustomers = "searchCustomers";
    /// <summary>Gets one product.</summary>
    public const string GetProduct = "getProduct";
    /// <summary>Searches products.</summary>
    public const string SearchProducts = "searchProducts";
    /// <summary>Gets one order.</summary>
    public const string GetOrder = "getOrder";
    /// <summary>Searches orders.</summary>
    public const string SearchOrders = "searchOrders";

    /// <summary>Gets every allowlisted operation name.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        GetCustomer, SearchCustomers, GetProduct, SearchProducts, GetOrder, SearchOrders
    };
}

/// <summary>Represents a deliberately safe error message that can be returned by an MCP tool.</summary>
public sealed class SafeToolException(string message) : Exception(message)
{
    /// <summary>Creates a safe not-found message for the requested resource type.</summary>
    public static SafeToolException NotFound(string resourceName) => new($"{resourceName} was not found.");
}

/// <summary>Maps known application failures to safe tool errors and hides unexpected exception details.</summary>
public static class McpToolGuard
{
    /// <summary>Executes an application operation and maps failures to tool-safe messages.</summary>
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (SafeToolException)
        {
            throw;
        }
        catch (RequestValidationException exception)
        {
            throw new SafeToolException(exception.Message);
        }
        catch (ConflictException exception)
        {
            throw new SafeToolException(exception.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new SafeToolException("The commerce tool could not complete the request.");
        }
    }
}
