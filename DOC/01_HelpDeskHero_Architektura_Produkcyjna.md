# 01 - Architektura produkcyjna

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. Docelowy układ solution

```text
HelpDeskHero
├─ src
│  ├─ HelpDeskHero.Api
│  ├─ HelpDeskHero.UI
│  └─ HelpDeskHero.Shared
├─ tests
│  ├─ HelpDeskHero.Api.IntegrationTests
│  └─ HelpDeskHero.UI.Tests
├─ docs
├─ .vscode
├─ .github/workflows
└─ HelpDeskHero.sln
```

## 2. Tworzenie solution

```powershell
mkdir C:\Users\blad\Documents\GitHub\HelpDeskHero
cd C:\Users\blad\Documents\GitHub\HelpDeskHero
mkdir src, tests, docs, .vscode
mkdir .github
mkdir .github\workflows

dotnet new sln -n HelpDeskHero

dotnet new classlib -n HelpDeskHero.Shared -o .\src\HelpDeskHero.Shared -f net10.0
dotnet new webapi -n HelpDeskHero.Api -o .\src\HelpDeskHero.Api -f net10.0
dotnet new blazorwasm -n HelpDeskHero.UI -o .\src\HelpDeskHero.UI -f net10.0

dotnet sln .\HelpDeskHero.slnx add .\src\HelpDeskHero.Shared\HelpDeskHero.Shared.csproj
dotnet sln .\HelpDeskHero.slnx add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj
dotnet sln .\HelpDeskHero.slnx add .\src\HelpDeskHero.UI\HelpDeskHero.UI.csproj

dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj reference .\src\HelpDeskHero.Shared\HelpDeskHero.Shared.csproj
dotnet add .\src\HelpDeskHero.UI\HelpDeskHero.UI.csproj reference .\src\HelpDeskHero.Shared\HelpDeskHero.Shared.csproj
```

## 3. Pakiety API

```powershell
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Swashbuckle.AspNetCore
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Hangfire.AspNetCore
dotnet add .\src\HelpDeskHero.Api\HelpDeskHero.Api.csproj package Hangfire.SqlServer
```

## 4. Struktura API

```text
HelpDeskHero.Api
├─ Application
│  ├─ Auth
│  ├─ Tickets
│  ├─ Workflow
│  ├─ Audit
│  ├─ Outbox
│  ├─ Dashboard
│  ├─ KnowledgeBase
│  ├─ Webhooks
│  ├─ Kpi
│  └─ Common
├─ BackgroundJobs
├─ Controllers
├─ Domain
├─ Hubs
├─ Infrastructure
│  ├─ Auth
│  ├─ Persistence
│  ├─ Notifications
│  ├─ Storage
│  └─ Tenancy
├─ Middleware
└─ Program.cs
```

## 5. Zasady

- Kontroler jest cienki: routing, DTO, autoryzacja, odpowiedź HTTP.
- Logika przypadków użycia jest w `Application Services`.
- Kontroler nie używa bezpośrednio `AppDbContext`, Hangfire, SignalR, audit ani outboxa.
- Encje są tylko w API.
- DTO są w Shared.
- UI rozmawia tylko z API.
- Logika biznesowa nie siedzi w Blazorze.
- Procesy w tle są w Hangfire.
- Integracje idą przez outbox.
- SignalR służy do powiadamiania UI, a nie do utrzymywania stanu systemu.

## 6. appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HelpDeskHeroDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "HelpDeskHero.Api",
    "Audience": "HelpDeskHero.UI",
    "Key": "CHANGE_ME_DEV_KEY_MINIMUM_64_CHARS_1234567890_ABCDEFGHIJKLMNOPQRSTUVWXYZ",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },
  "AllowedHosts": "*"
}
```

## 7. Program.cs - szkic docelowy

```csharp
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* TokenValidationParameters */ });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageTickets", p => p.RequireRole("Admin", "Agent"));
    options.AddPolicy("CanViewAudit", p => p.RequireRole("Admin"));
});

builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();
builder.Services.AddSignalR();

// Application services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IClock, SystemClock>();
builder.Services.AddScoped<ITicketApplicationService, TicketApplicationService>();
builder.Services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
builder.Services.AddScoped<IWorkflowApplicationService, WorkflowApplicationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IDashboardApplicationService, DashboardApplicationService>();
builder.Services.AddScoped<IKnowledgeBaseApplicationService, KnowledgeBaseApplicationService>();
builder.Services.AddScoped<IWebhookSubscriptionService, WebhookSubscriptionService>();
builder.Services.AddScoped<IKpiApplicationService, KpiApplicationService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHangfireDashboard("/hangfire");
app.MapHub<TicketHub>("/hubs/tickets");
app.Run();

public partial class Program { }
```

## Seed danych produkcyjnych

Seed danych startowych nie znajduje się w kontrolerach, `AppDbContext` ani w jednym wielkim `DbInitializer`.

Przyjmujemy osobną warstwę:

```text
Infrastructure/Persistence/Seeding
```

Zasada:

```text
DatabaseSeeder = orkiestrator
ISeedStep = pojedynczy krok seeda
TenantSeedStep / WorkflowSeedStep / SlaPolicySeedStep = osobne klasy odpowiedzialności
```

Seed w produkcji uruchamia konto migracyjne, nie konto aplikacji runtime.

Szczegóły są w pliku `02E_HelpDeskHero_EFCore_Production_Seed_Data.md`.
