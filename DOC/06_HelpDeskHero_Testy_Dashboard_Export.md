# 06 - Testy, dashboard i export CSV

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. Testy

Tworzymy dwa projekty:

```powershell
dotnet new xunit -n HelpDeskHero.Api.IntegrationTests -o .\tests\HelpDeskHero.Api.IntegrationTests -f net10.0
dotnet new xunit -n HelpDeskHero.UI.Tests -o .\tests\HelpDeskHero.UI.Tests -f net10.0

dotnet add .\tests\HelpDeskHero.Api.IntegrationTests\HelpDeskHero.Api.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add .\tests\HelpDeskHero.Api.IntegrationTests\HelpDeskHero.Api.IntegrationTests.csproj package FluentAssertions
dotnet add .\tests\HelpDeskHero.Api.IntegrationTests\HelpDeskHero.Api.IntegrationTests.csproj package Microsoft.EntityFrameworkCore.InMemory

dotnet add .\tests\HelpDeskHero.UI.Tests\HelpDeskHero.UI.Tests.csproj package bunit
dotnet add .\tests\HelpDeskHero.UI.Tests\HelpDeskHero.UI.Tests.csproj package FluentAssertions
```

## 2. Dashboard DTO

```csharp
public sealed class DashboardSummaryDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int DeletedTickets { get; set; }
    public int HighPriorityOpenTickets { get; set; }
    public List<RecentAuditItemDto> RecentAuditItems { get; set; } = [];
}
```

## 3. Dashboard endpoint

```text
GET /api/dashboard/summary
```

Zasady:

- `TotalTickets` liczymy z `IgnoreQueryFilters()`.
- `DeletedTickets` również z `IgnoreQueryFilters()`.
- `OpenTickets` liczymy tylko z aktywnych ticketów.
- `RecentAuditItems` pokazuje ostatnie 10 operacji.

## 4. CSV export

Endpoint:

```text
GET /api/tickets/export?status=New&priority=High
```

Zasady:

- dostęp przez policy `CanManageTickets`,
- escape wartości tekstowych,
- wynik jako `text/csv`,
- nazwa pliku `tickets.csv`.

## 5. Minimalna checklist testów

API:

- [ ] login zwraca token,
- [ ] endpoint chroniony bez tokena zwraca `401`,
- [ ] create ticket działa dla `Admin/Agent`,
- [ ] create ticket nie działa dla zwykłego `User`,
- [ ] update ze starą RowVersion zwraca `409`,
- [ ] soft delete ukrywa ticket,
- [ ] restore przywraca ticket.

UI:

- [ ] `Login.razor` renderuje formularz,
- [ ] `Tickets.razor` pokazuje tabelę,
- [ ] błąd API pokazuje komunikat,
- [ ] przycisk admina jest ukryty dla użytkownika bez roli.

## 6. Uruchomienie

```powershell
dotnet test .\HelpDeskHero.sln
```
