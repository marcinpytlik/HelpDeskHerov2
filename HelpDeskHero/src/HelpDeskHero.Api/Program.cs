using HelpDeskHero.Api.Extensions;
using HelpDeskHero.Infrastructure.DependencyInjection;
using HelpDeskHero.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await app.SeedDatabaseAsync();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program
{
}