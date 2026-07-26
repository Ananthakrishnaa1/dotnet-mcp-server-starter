namespace CommerceMcpDemo.Domain;

/// <summary>Describes whether a customer can place new orders.</summary>
public enum CustomerStatus { Active, Inactive }

/// <summary>Describes the lifecycle state of an order.</summary>
public enum OrderStatus { Draft, Confirmed, Shipped, Cancelled }

/// <summary>Represents a customer held in the deterministic in-memory catalog.</summary>
public sealed class Customer
{
    /// <summary>Gets or initializes the stable customer identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets or initializes the display name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets or initializes the unique email address.</summary>
    public required string Email { get; init; }
    /// <summary>Gets or initializes the customer's availability status.</summary>
    public CustomerStatus Status { get; init; }
    /// <summary>Gets or initializes the UTC creation date.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

/// <summary>Represents a catalog product held in the deterministic in-memory catalog.</summary>
public sealed class Product
{
    /// <summary>Gets or initializes the stable product identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets or initializes the human-readable stock keeping unit.</summary>
    public required string Sku { get; init; }
    /// <summary>Gets or initializes the product name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets or initializes the unit price.</summary>
    public decimal Price { get; init; }
    /// <summary>Gets or initializes the currently available stock.</summary>
    public int StockQuantity { get; init; }
    /// <summary>Gets or initializes whether the product is discoverable.</summary>
    public bool IsActive { get; init; }
}

/// <summary>Represents one line in an order.</summary>
public sealed class OrderItem
{
    /// <summary>Gets or initializes the referenced product identifier.</summary>
    public Guid ProductId { get; init; }
    /// <summary>Gets or initializes the ordered quantity.</summary>
    public int Quantity { get; init; }
    /// <summary>Gets or initializes the price captured for this order line.</summary>
    public decimal UnitPrice { get; init; }
}

/// <summary>Represents a commerce order and its immutable line items.</summary>
public sealed class Order
{
    /// <summary>Gets or initializes the stable order identifier.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets or initializes the purchasing customer identifier.</summary>
    public Guid CustomerId { get; init; }
    /// <summary>Gets or initializes the current order status.</summary>
    public OrderStatus Status { get; init; }
    /// <summary>Gets or initializes the UTC creation date.</summary>
    public DateTime CreatedAtUtc { get; init; }
    /// <summary>Gets or initializes the order lines.</summary>
    public required IReadOnlyList<OrderItem> Items { get; init; }
    /// <summary>Gets the deterministic total calculated from the order lines.</summary>
    public decimal Total => Items.Sum(item => item.Quantity * item.UnitPrice);
}
