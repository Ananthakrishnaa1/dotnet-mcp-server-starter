using CommerceMcpDemo.Application;
using CommerceMcpDemo.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddCommerceApplication();
builder.Services.AddCommerceInMemoryData();

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
    var problem = new ProblemDetails
    {
        Status = statusCode,
        Title = statusCode == StatusCodes.Status500InternalServerError ? "Unexpected server error" : "Request could not be completed",
        Detail = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception?.Message
    };
    await Results.Problem(problem.Detail, statusCode: problem.Status, title: problem.Title).ExecuteAsync(context);
}));
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "CommerceMcpDemo API v1"));
app.MapControllers();
app.Run();

/// <summary>Provides an entry point type for API integration tests.</summary>
public partial class Program;
