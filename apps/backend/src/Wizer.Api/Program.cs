using Scalar.AspNetCore;
using Wizer.Api.Infrastructure;
using Wizer.Api.Infrastructure.Persistence;
using Wizer.Api.Modules.Campuses;
using Wizer.Api.Modules.Identity;
using Wizer.Api.Shared;
using Wizer.Api.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddWizerLogging();

builder.Services.AddShared();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCampusesModule();
builder.Services.AddIdentityModule();
builder.Services.AddWizerControllers();
builder.Services.AddOpenApi("v1");

var app = builder.Build();

await app.PrepareDatabaseAsync();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseWizerRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/api/docs");
}

app.MapHealthEndpoints();
app.MapControllers();

await app.RunAsync();
