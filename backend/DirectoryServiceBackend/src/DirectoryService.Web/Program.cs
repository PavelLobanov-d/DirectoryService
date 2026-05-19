using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();


app.MapGet("/", () => "Yellow Submarine");

app.MapControllers();

app.MapHealthChecks("/health");

if(!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync().ConfigureAwait(false);
