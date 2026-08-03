using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
namespace HelpDeskHero.IntegrationTests.Common;
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program> {
     protected override void ConfigureWebHost(IWebHostBuilder builder) 
     { builder.UseEnvironment("Development"); } 
}