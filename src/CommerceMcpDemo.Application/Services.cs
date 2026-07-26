using CommerceMcpDemo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceMcpDemo.Application;

/// <summary>Registers application services used by both host adapters.</summary>
public static class DependencyInjection
{
    /// <summary>Adds stateless application services to a service collection.</summary>
    public static IServiceCollection AddCommerceApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}

/// <summary>Implements customer business rules independently of HTTP and MCP.</summary>
public sealed class CustomerService(ICustomerRepository customers) : ICustomerService
{
    /// <inheritdoc />
    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);
        return customer is null ? null : customer.ToDto();
    }

    /// <inheritdoc />
    public async Task<PagedResult<CustomerDto>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken)
    {
        var page = request.ValidatePage();
        var query = (request.Query ?? string.Empty).Trim();
        var values = await customers.GetAllAsync(cancellationToken);
        var filtered = values
            .Where(customer => request.Status is null || customer.Status == request.Status)
            .Where(customer => query.Length == 0 || customer.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || customer.Email.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(customer => customer.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(customer => customer.Id)
            .Select(customer => customer.ToDto());
        return filtered.ToPage(page);
    }

    /// <inheritdoc />
    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RequestValidationException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            throw new RequestValidationException("A valid customer email is required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await customers.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A customer with that email already exists.");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            Status = request.Status,
            CreatedAtUtc = DateTime.UtcNow
        };
        await customers.AddAsync(customer, cancellationToken);
        return customer.ToDto();
    }
}

/// <summary>Implements product read business rules independently of HTTP and MCP.</summary>
public sealed class ProductService(IProductRepository products) : IProductService
{
    /// <inheritdoc />
    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        return product is null ? null : product.ToDto();
    }

    /// <inheritdoc />
    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var page = request.ValidatePage();
        if (request.MaxPrice is < 0)
        {
            throw new RequestValidationException("Maximum price cannot be negative.");
        }

        var query = (request.Query ?? string.Empty).Trim();
        var values = await products.GetAllAsync(cancellationToken);
        var filtered = values
            .Where(product => request.IsActive is null || product.IsActive == request.IsActive)
            .Where(product => request.InStock is null || (product.StockQuantity > 0) == request.InStock)
            .Where(product => request.MaxPrice is null || product.Price <= request.MaxPrice)
            .Where(product => query.Length == 0 || product.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || product.Sku.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(product => product.Id)
            .Select(product => product.ToDto());
        return filtered.ToPage(page);
    }
}

/// <summary>Implements order rules independently of HTTP and MCP.</summary>
public sealed class OrderService(IOrderRepository orders, ICustomerRepository customers, IProductRepository products) : IOrderService
{
    /// <inheritdoc />
    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        return order is null ? null : order.ToDto();
    }

    /// <inheritdoc />
    public async Task<PagedResult<OrderDto>> SearchAsync(OrderSearchRequest request, CancellationToken cancellationToken)
    {
        var page = request.ValidatePage();
        var values = await orders.GetAllAsync(cancellationToken);
        var filtered = values
            .Where(order => request.CustomerId is null || order.CustomerId == request.CustomerId)
            .Where(order => request.Status is null || order.Status == request.Status)
            .Where(order => request.CreatedAfterUtc is null || order.CreatedAtUtc >= request.CreatedAfterUtc)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .Select(order => order.ToDto());
        return filtered.ToPage(page);
    }

    /// <inheritdoc />
    public async Task<OrderDto> CreateDraftAsync(CreateDraftOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new RequestValidationException("A draft order must contain at least one item.");
        }

        var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new RequestValidationException("The customer does not exist.");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            throw new RequestValidationException("Only active customers can create draft orders.");
        }

        var items = new List<OrderItem>();
        foreach (var requestItem in request.Items)
        {
            if (requestItem.Quantity <= 0)
            {
                throw new RequestValidationException("Order item quantity must be greater than zero.");
            }

            var product = await products.GetByIdAsync(requestItem.ProductId, cancellationToken);
            if (product is null || !product.IsActive)
            {
                throw new RequestValidationException("Every order item must reference an active product.");
            }

            if (product.StockQuantity < requestItem.Quantity)
            {
                throw new RequestValidationException("Requested quantity exceeds available stock.");
            }

            items.Add(new OrderItem { ProductId = product.Id, Quantity = requestItem.Quantity, UnitPrice = product.Price });
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = OrderStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            Items = items
        };
        await orders.AddAsync(order, cancellationToken);
        return order.ToDto();
    }
}

/// <summary>Contains deterministic DTO mapping helpers.</summary>
public static class MappingExtensions
{
    /// <summary>Maps a customer entity to a response DTO.</summary>
    public static CustomerDto ToDto(this Customer customer) => new(customer.Id, customer.Name, customer.Email, customer.Status, customer.CreatedAtUtc);

    /// <summary>Maps a product entity to a response DTO.</summary>
    public static ProductDto ToDto(this Product product) => new(product.Id, product.Sku, product.Name, product.Price, product.StockQuantity, product.IsActive);

    /// <summary>Maps an order entity to a response DTO.</summary>
    public static OrderDto ToDto(this Order order) => new(order.Id, order.CustomerId, order.Status, order.Total, order.CreatedAtUtc, order.Items.Select(item => new OrderItemDto(item.ProductId, item.Quantity, item.UnitPrice)).ToArray());
}

/// <summary>Contains validation and paging helpers shared by services.</summary>
public static class PagingExtensions
{
    /// <summary>Validates an unbounded request and returns a safe page request.</summary>
    public static PageRequest ValidatePage(this CustomerSearchRequest request) => new PageRequest(request.Page, request.PageSize).ValidatePage();

    /// <summary>Validates an unbounded request and returns a safe page request.</summary>
    public static PageRequest ValidatePage(this ProductSearchRequest request) => new PageRequest(request.Page, request.PageSize).ValidatePage();

    /// <summary>Validates an unbounded request and returns a safe page request.</summary>
    public static PageRequest ValidatePage(this OrderSearchRequest request) => new PageRequest(request.Page, request.PageSize).ValidatePage();

    /// <summary>Validates page bounds and applies the global maximum page size.</summary>
    public static PageRequest ValidatePage(this PageRequest request)
    {
        if (request.Page < 1)
        {
            throw new RequestValidationException("Page must be at least 1.");
        }

        if (request.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException("Page size must be between 1 and 100.");
        }

        return request;
    }

    /// <summary>Creates a deterministic page from an already sorted sequence.</summary>
    public static PagedResult<T> ToPage<T>(this IEnumerable<T> source, PageRequest page)
    {
        var values = source.ToArray();
        return new PagedResult<T>(values.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToArray(), page.Page, page.PageSize, values.Length);
    }
}
