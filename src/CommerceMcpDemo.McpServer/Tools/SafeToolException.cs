using CommerceMcpDemo.Application;

namespace CommerceMcpDemo.McpServer.Tools;

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
