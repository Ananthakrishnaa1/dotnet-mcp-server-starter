using CommerceMcpDemo.Domain;

namespace CommerceMcpDemo.Application;

/// <summary>Represents a bounded page of values.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Supplies common pagination parameters for list operations.</summary>
public sealed record PageRequest(int Page = 1, int PageSize = 20);

/// <summary>Supplies filters for customer search operations.</summary>
public sealed record CustomerSearchRequest(string? Query = null, CustomerStatus? Status = null, int Page = 1, int PageSize = 20);

/// <summary>Supplies values for a new transient customer.</summary>
public sealed record CreateCustomerRequest(string Name, string Email, CustomerStatus Status = CustomerStatus.Active);

/// <summary>Supplies filters for product search operations.</summary>
public sealed record ProductSearchRequest(string? Query = null, bool? IsActive = true, bool? InStock = null, decimal? MaxPrice = null, int Page = 1, int PageSize = 20);

/// <summary>Supplies filters for order search operations.</summary>
public sealed record OrderSearchRequest(Guid? CustomerId = null, OrderStatus? Status = null, DateTime? CreatedAfterUtc = null, int Page = 1, int PageSize = 20);

/// <summary>Supplies a single requested product and quantity for a draft order.</summary>
public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);

/// <summary>Supplies values for a new transient draft order.</summary>
public sealed record CreateDraftOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderItemRequest> Items);

/// <summary>Returns customer data without exposing domain entities.</summary>
public sealed record CustomerDto(Guid Id, string Name, string Email, CustomerStatus Status, DateTime CreatedAtUtc);

/// <summary>Returns product data without exposing domain entities.</summary>
public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, int StockQuantity, bool IsActive);

/// <summary>Returns an order line for an API or MCP response.</summary>
public sealed record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);

/// <summary>Returns order data with its line items.</summary>
public sealed record OrderDto(Guid Id, Guid CustomerId, OrderStatus Status, decimal Total, DateTime CreatedAtUtc, IReadOnlyList<OrderItemDto> Items);

/// <summary>Defines customer operations shared by the API and MCP tools.</summary>
public interface ICustomerService
{
    /// <summary>Finds a customer by identifier, or returns null when it does not exist.</summary>
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Searches customers with validated bounded pagination.</summary>
    Task<PagedResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken);
    /// <summary>Creates a customer only in the current host's in-memory store.</summary>
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
}

/// <summary>Defines product operations shared by the API and MCP tools.</summary>
public interface IProductService
{
    /// <summary>Finds a product by identifier, or returns null when it does not exist.</summary>
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Searches products with validated bounded pagination.</summary>
    Task<PagedResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);
}

/// <summary>Defines order operations shared by the API and MCP tools.</summary>
public interface IOrderService
{
    /// <summary>Finds an order by identifier, or returns null when it does not exist.</summary>
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Searches orders with validated bounded pagination.</summary>
    Task<PagedResult<OrderDto>> SearchAsync(OrderSearchRequest request, CancellationToken cancellationToken);
    /// <summary>Creates a draft order only in the current host's in-memory store.</summary>
    Task<OrderDto> CreateDraftAsync(CreateDraftOrderRequest request, CancellationToken cancellationToken);
}

/// <summary>Defines persistence-independent customer access.</summary>
public interface ICustomerRepository
{
    /// <summary>Reads one customer by identifier.</summary>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Reads all customers for service-level filtering and paging.</summary>
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken);
    /// <summary>Checks whether an email address already exists.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    /// <summary>Adds a customer to the current in-memory store.</summary>
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
}

/// <summary>Defines persistence-independent product access.</summary>
public interface IProductRepository
{
    /// <summary>Reads one product by identifier.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Reads all products for service-level filtering and paging.</summary>
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);
}

/// <summary>Defines persistence-independent order access.</summary>
public interface IOrderRepository
{
    /// <summary>Reads one order by identifier.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Reads all orders for service-level filtering and paging.</summary>
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken);
    /// <summary>Adds an order to the current in-memory store.</summary>
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
