# CommerceMcpDemo

A .NET 10 sample that exposes a layered commerce API and six read-only MCP tools. It uses no database: all catalog values are deterministic, hardcoded C# values held in a singleton `HardcodedCommerceDataStore`.

## Architecture

`CommerceMcpDemo.Domain` contains the commerce entities. `CommerceMcpDemo.Application` owns DTOs, validation, service interfaces, and service implementations. `CommerceMcpDemo.Infrastructure` implements the repository interfaces over the hardcoded store. The API controllers and MCP tool adapters call only application-service interfaces; neither calls the other over HTTP.

Both adapters use `AddCommerceApplication()` and `AddCommerceInMemoryData()`. The MCP executable is a combined host: it loads the API controllers and MCP stdio transport into one DI container, so controllers and tools share the exact same `HardcodedCommerceDataStore` instance. This is the only way to share an in-memory instance: separate OS processes cannot share managed heap memory. `scripts/start-api.sh` is deliberately HTTP-only and creates its own process-local copy for API-only development.

## macOS setup

The repository is pinned to .NET SDK 10.0.302 in `global.json`. On Apple Silicon, install the Arm64 .NET 10 SDK; on Intel, install the x64 SDK. VS Code plus C# Dev Kit, GitHub Copilot, and Git are also required.

```bash
./.dotnet/dotnet --info
./.dotnet/dotnet restore CommerceMcpDemo.slnx
./.dotnet/dotnet build CommerceMcpDemo.slnx --no-restore
./.dotnet/dotnet test CommerceMcpDemo.slnx --no-build
```

If you installed .NET 10 system-wide instead, replace `./.dotnet/dotnet` with `dotnet`. The scripts automatically prefer the local SDK when it exists.

## Run the API

Run the HTTP-only API with:

```bash
scripts/start-api.sh
```

OpenAPI is served at `/openapi/v1.json`. Swagger JSON is served at `/swagger/v1/swagger.json` and the interactive Swagger UI is at `/swagger`. The API routes are `/api/customers`, `/api/products`, and `/api/orders`.

## Run MCP in VS Code

The workspace file `.vscode/mcp.json` launches `scripts/start-mcp.sh`. That script builds with stdout redirected to stderr, then starts the combined API/MCP host at `http://127.0.0.1:5057` by default. Set `Commerce__HttpUrl` to change that address. MCP protocol messages are the only stdout output; application and Kestrel logs use stderr.

Restart VS Code or refresh MCP tools after changing a tool definition.

## Sample data and transient writes

The store contains 10 customers, 20 products, and 15 orders with stable GUIDs. Any `POST` customer or draft-order data exists only in the current host process. Restart the host to reset all sample data—there is no database, migration, connection string, or reset script.

## Add an MCP tool

1. Add an application-service operation and test it.
2. Add an allowlisted operation in `CommerceMcpDemo.McpServer/Tools/Tools.cs`; it must call an application-service interface and use `McpToolGuard` for safe errors.
3. Add the MCP name, description, input JSON Schema, and operation name to `src/CommerceMcpDemo.McpServer/tools.json`.
4. Add tests for the new operation and configuration.

`tools.json` controls which allowlisted operations are exposed, their MCP names, descriptions, schemas, and enablement. It cannot execute arbitrary methods or code. Restart the MCP server after changing the file so the startup build copies it to the executable output directory.

## Troubleshooting

- `dotnet` selects SDK 9: use `./.dotnet/dotnet` or install SDK 10.0.302.
- Copilot cannot communicate with the server: inspect stderr for build and runtime logs; do not write logging or startup banners to stdout.
- A tool is missing: restart the MCP server after its attributes or signature change.
- Data unexpectedly changed: restart the relevant host to restore the hardcoded catalog.

## MCP SDK version

`ModelContextProtocol` is pinned to prerelease `2.0.0-rc.1` in `Directory.Packages.props`. It is intentionally not a floating version. Upgrade only after reviewing the official v2 release notes and API documentation, then run the full restore, build, and test suite.

## Example Copilot prompts

- `commerce_get_customer`: “Get customer 00000000-0000-0000-0000-000000000001.”
- `commerce_search_customers`: “Find active customers with ‘an’ in their name or email.”
- `commerce_get_product`: “Get product 10000000-0000-0000-0000-000000000001.”
- `commerce_search_products`: “Show in-stock active products under 50.”
- `commerce_get_order`: “Get order 20000000-0000-0000-0000-000000000001.”
- `commerce_search_orders`: “List the five newest confirmed orders.”
