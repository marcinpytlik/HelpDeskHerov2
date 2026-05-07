# 07 - Hangfire, powiadomienia, komentarze, załączniki, CI/CD

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. Hangfire

Hangfire służy do procesów w tle:

- daily summary,
- powiadomienia po utworzeniu ticketu,
- przetwarzanie outboxa,
- kontrola SLA,
- retry integracji.

Rejestracja:

```csharp
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();
app.MapHangfireDashboard("/hangfire");
```

## 2. NotificationJob

```csharp
public interface INotificationJob
{
    Task SendTicketCreatedNotificationsAsync(int ticketId, CancellationToken ct = default);
    Task SendDailySummaryAsync(CancellationToken ct = default);
}
```

## 3. Notification dispatcher

Kanały:

```text
Email
Webhook
InApp
```

Interfejsy:

```csharp
public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task SendAsync(NotificationMessage message, CancellationToken ct = default);
}

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationMessage message, CancellationToken ct = default);
}
```

## 4. Komentarze

Endpointy:

```text
GET  /api/tickets/{id}/comments
POST /api/tickets/{id}/comments
```

DTO:

```csharp
public sealed class CreateTicketCommentDto
{
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}
```

## 5. Załączniki

Endpointy:

```text
POST /api/tickets/{id}/attachments
GET  /api/tickets/{id}/attachments/{attachmentId}
```

Zasady:

- maksymalny rozmiar pliku,
- whitelist rozszerzeń,
- zapis poza `wwwroot`,
- w bazie tylko metadane i ścieżka,
- pobieranie tylko po sprawdzeniu uprawnień do ticketu.

## 6. CI/CD - GitHub Actions

```yaml
name: dotnet-build-test

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore .\HelpDeskHero.sln
      - name: Build
        run: dotnet build .\HelpDeskHero.sln --configuration Release --no-restore
      - name: Test
        run: dotnet test .\HelpDeskHero.sln --configuration Release --no-build
```
