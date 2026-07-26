using CommerceMcpDemo.Application;
using CommerceMcpDemo.Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace CommerceMcpDemo.Infrastructure;

/// <summary>Registers the one in-memory data store shared by all adapters in a host process.</summary>
public static class DependencyInjection
{
    /// <summary>Adds one deterministic singleton store and its repository adapters.</summary>
    public static IServiceCollection AddCommerceInMemoryData(this IServiceCollection services)
    {
        services.AddSingleton<HardcodedCommerceDataStore>();
        services.AddSingleton<ICustomerRepository, CustomerRepository>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IOrderRepository, OrderRepository>();
        return services;
    }
}

/// <summary>Owns the hardcoded catalog and serializes access to its transient collections.</summary>
public sealed class HardcodedCommerceDataStore
{
    private readonly object syncRoot = new();
    private readonly List<Customer> customers = CreateCustomers();
    private readonly List<Product> products = CreateProducts();
    private readonly List<Order> orders = CreateOrders();

    /// <summary>Returns an exclusive lock used by the repository implementations.</summary>
    internal object SyncRoot => syncRoot;
    /// <summary>Returns the mutable customer collection owned by this singleton.</summary>
    internal List<Customer> Customers => customers;
    /// <summary>Returns the mutable product collection owned by this singleton.</summary>
    internal List<Product> Products => products;
    /// <summary>Returns the mutable order collection owned by this singleton.</summary>
    internal List<Order> Orders => orders;

    /// <summary>Creates ten deterministic customer records.</summary>
    private static List<Customer> CreateCustomers() =>
    [
        Customer("00000000-0000-0000-0000-000000000001", "Anika Shah", "anika@example.test", CustomerStatus.Active, "2026-01-03"),
        Customer("00000000-0000-0000-0000-000000000002", "Ben Carter", "ben@example.test", CustomerStatus.Active, "2026-01-06"),
        Customer("00000000-0000-0000-0000-000000000003", "Carla Mendes", "carla@example.test", CustomerStatus.Inactive, "2026-01-09"),
        Customer("00000000-0000-0000-0000-000000000004", "Daniel Kim", "daniel@example.test", CustomerStatus.Active, "2026-01-11"),
        Customer("00000000-0000-0000-0000-000000000005", "Elena Rossi", "elena@example.test", CustomerStatus.Active, "2026-01-14"),
        Customer("00000000-0000-0000-0000-000000000006", "Farah Khan", "farah@example.test", CustomerStatus.Inactive, "2026-01-18"),
        Customer("00000000-0000-0000-0000-000000000007", "Gavin Lee", "gavin@example.test", CustomerStatus.Active, "2026-01-21"),
        Customer("00000000-0000-0000-0000-000000000008", "Hana Patel", "hana@example.test", CustomerStatus.Active, "2026-01-24"),
        Customer("00000000-0000-0000-0000-000000000009", "Ivan Petrov", "ivan@example.test", CustomerStatus.Active, "2026-01-27"),
        Customer("00000000-0000-0000-0000-000000000010", "Jules Martin", "jules@example.test", CustomerStatus.Inactive, "2026-01-30")
    ];

    /// <summary>Creates twenty deterministic product records.</summary>
    private static List<Product> CreateProducts() =>
    [
        Product("10000000-0000-0000-0000-000000000001", "LMP-001", "Aurora Lamp", 49.99m, 14, true),
        Product("10000000-0000-0000-0000-000000000002", "MUG-002", "Basalt Mug", 18.50m, 0, true),
        Product("10000000-0000-0000-0000-000000000003", "BAG-003", "Canvas Market Bag", 24.00m, 20, true),
        Product("10000000-0000-0000-0000-000000000004", "DSK-004", "Desk Organizer", 35.75m, 8, true),
        Product("10000000-0000-0000-0000-000000000005", "EAR-005", "Echo Earbuds", 89.00m, 11, true),
        Product("10000000-0000-0000-0000-000000000006", "FRM-006", "Fjord Picture Frame", 16.25m, 3, true),
        Product("10000000-0000-0000-0000-000000000007", "GRD-007", "Granite Coaster Set", 28.00m, 7, true),
        Product("10000000-0000-0000-0000-000000000008", "HOD-008", "Harbor Hoodie", 64.99m, 12, true),
        Product("10000000-0000-0000-0000-000000000009", "INK-009", "Indigo Notebook", 12.99m, 30, true),
        Product("10000000-0000-0000-0000-000000000010", "JAR-010", "Juniper Storage Jar", 21.00m, 0, true),
        Product("10000000-0000-0000-0000-000000000011", "KEY-011", "Kinetic Keyboard", 99.00m, 4, true),
        Product("10000000-0000-0000-0000-000000000012", "LID-012", "Linen Throw", 42.50m, 9, true),
        Product("10000000-0000-0000-0000-000000000013", "MIR-013", "Mica Hand Mirror", 31.25m, 6, true),
        Product("10000000-0000-0000-0000-000000000014", "NOM-014", "Nomad Water Bottle", 27.99m, 17, true),
        Product("10000000-0000-0000-0000-000000000015", "ORB-015", "Orbit Clock", 75.00m, 2, true),
        Product("10000000-0000-0000-0000-000000000016", "PEN-016", "Pine Pencil Case", 14.75m, 18, true),
        Product("10000000-0000-0000-0000-000000000017", "QRT-017", "Quartz Planter", 39.00m, 5, true),
        Product("10000000-0000-0000-0000-000000000018", "RUG-018", "River Runner Rug", 129.00m, 1, false),
        Product("10000000-0000-0000-0000-000000000019", "SND-019", "Sandalwood Candle", 22.00m, 0, false),
        Product("10000000-0000-0000-0000-000000000020", "TRV-020", "Travel Cutlery Set", 19.50m, 15, true)
    ];

    /// <summary>Creates fifteen deterministic orders from the hardcoded catalog identifiers.</summary>
    private static List<Order> CreateOrders() =>
    [
        Order("20000000-0000-0000-0000-000000000001", 1, OrderStatus.Confirmed, "2026-02-01", (1, 2)),
        Order("20000000-0000-0000-0000-000000000002", 2, OrderStatus.Shipped, "2026-02-03", (3, 1)),
        Order("20000000-0000-0000-0000-000000000003", 4, OrderStatus.Draft, "2026-02-05", (5, 1)),
        Order("20000000-0000-0000-0000-000000000004", 5, OrderStatus.Cancelled, "2026-02-07", (6, 2)),
        Order("20000000-0000-0000-0000-000000000005", 7, OrderStatus.Confirmed, "2026-02-09", (8, 1)),
        Order("20000000-0000-0000-0000-000000000006", 8, OrderStatus.Shipped, "2026-02-11", (9, 3)),
        Order("20000000-0000-0000-0000-000000000007", 9, OrderStatus.Confirmed, "2026-02-13", (11, 1)),
        Order("20000000-0000-0000-0000-000000000008", 1, OrderStatus.Draft, "2026-02-15", (12, 1)),
        Order("20000000-0000-0000-0000-000000000009", 2, OrderStatus.Confirmed, "2026-02-17", (14, 2)),
        Order("20000000-0000-0000-0000-000000000010", 4, OrderStatus.Shipped, "2026-02-19", (15, 1)),
        Order("20000000-0000-0000-0000-000000000011", 5, OrderStatus.Cancelled, "2026-02-21", (16, 2)),
        Order("20000000-0000-0000-0000-000000000012", 7, OrderStatus.Confirmed, "2026-02-23", (17, 1)),
        Order("20000000-0000-0000-0000-000000000013", 8, OrderStatus.Draft, "2026-02-25", (20, 2)),
        Order("20000000-0000-0000-0000-000000000014", 9, OrderStatus.Shipped, "2026-02-27", (1, 1)),
        Order("20000000-0000-0000-0000-000000000015", 2, OrderStatus.Confirmed, "2026-03-01", (5, 1))
    ];

    /// <summary>Builds a customer from a literal stable identifier and date.</summary>
    private static Customer Customer(string id, string name, string email, CustomerStatus status, string createdAtUtc) => new() { Id = Guid.Parse(id), Name = name, Email = email, Status = status, CreatedAtUtc = DateTime.SpecifyKind(DateTime.Parse(createdAtUtc, CultureInfo.InvariantCulture), DateTimeKind.Utc) };

    /// <summary>Builds a product from literal catalog values.</summary>
    private static Product Product(string id, string sku, string name, decimal price, int stockQuantity, bool isActive) => new() { Id = Guid.Parse(id), Sku = sku, Name = name, Price = price, StockQuantity = stockQuantity, IsActive = isActive };

    /// <summary>Builds an order that references one hardcoded product.</summary>
    private static Order Order(string id, int customerNumber, OrderStatus status, string createdAtUtc, (int ProductNumber, int Quantity) item)
    {
        var product = CreateProducts().Single(value => value.Id == Guid.Parse($"10000000-0000-0000-0000-{item.ProductNumber:000000000000}"));
        return new Order
        {
            Id = Guid.Parse(id),
            CustomerId = Guid.Parse($"00000000-0000-0000-0000-{customerNumber:000000000000}"),
            Status = status,
            CreatedAtUtc = DateTime.SpecifyKind(DateTime.Parse(createdAtUtc, CultureInfo.InvariantCulture), DateTimeKind.Utc),
            Items = [new OrderItem { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = product.Price }]
        };
    }
}

/// <summary>Reads and writes customers through the shared singleton data store.</summary>
public sealed class CustomerRepository(HardcodedCommerceDataStore store) : ICustomerRepository
{
    /// <inheritdoc />
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) return Task.FromResult(store.Customers.SingleOrDefault(customer => customer.Id == id));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) return Task.FromResult<IReadOnlyList<Customer>>(store.Customers.ToArray());
    }

    /// <inheritdoc />
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) return Task.FromResult(store.Customers.Any(customer => string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) store.Customers.Add(customer);
        return Task.CompletedTask;
    }
}

/// <summary>Reads products through the shared singleton data store.</summary>
public sealed class ProductRepository(HardcodedCommerceDataStore store) : IProductRepository
{
    /// <inheritdoc />
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) return Task.FromResult(store.Products.SingleOrDefault(product => product.Id == id));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) return Task.FromResult<IReadOnlyList<Product>>(store.Products.ToArray());
    }
}

/// <summary>Reads and writes orders through the shared singleton data store.</summary>
public sealed class OrderRepository(HardcodedCommerceDataStore store) : IOrderRepository
{
    /// <inheritdoc />
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) return Task.FromResult(store.Orders.SingleOrDefault(order => order.Id == id));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) return Task.FromResult<IReadOnlyList<Order>>(store.Orders.ToArray());
    }

    /// <inheritdoc />
    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.SyncRoot) store.Orders.Add(order);
        return Task.CompletedTask;
    }
}
