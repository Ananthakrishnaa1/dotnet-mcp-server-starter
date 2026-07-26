namespace CommerceMcpDemo.Application;

/// <summary>Represents a safe, user-correctable input validation failure.</summary>
public sealed class RequestValidationException(string message) : Exception(message);

/// <summary>Represents a safe conflict caused by an already-existing value.</summary>
public sealed class ConflictException(string message) : Exception(message);
