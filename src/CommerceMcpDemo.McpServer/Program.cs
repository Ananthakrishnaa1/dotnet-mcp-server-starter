using CommerceMcpDemo.Application;
using CommerceMcpDemo.Api.Controllers;
using CommerceMcpDemo.Infrastructure;
using CommerceMcpDemo.McpServer.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Commerce:HttpUrl"] ?? "http://127.0.0.1:5057");
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddApplicationPart(typeof(CustomersController).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCommerceApplication();
builder.Services.AddCommerceInMemoryData();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(CustomerTools).Assembly);

var app = builder.Build();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var statusCode = exception switch
    {
        RequestValidationException => StatusCodes.Status400BadRequest,
        ConflictException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
    var title = statusCode == StatusCodes.Status500InternalServerError ? "Unexpected server error" : "Request could not be completed";
    var detail = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception?.Message;
    await Results.Problem(detail, statusCode: statusCode, title: title).ExecuteAsync(context);
}));
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "CommerceMcpDemo API v1"));
app.MapControllers();
await app.RunAsync();
