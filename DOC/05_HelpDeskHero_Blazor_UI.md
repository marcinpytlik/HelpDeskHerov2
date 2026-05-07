# 05 - Blazor WebAssembly UI

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. Zasada

Blazor WebAssembly nie zna encji EF Core. UI pracuje tylko z DTO z projektu `HelpDeskHero.Shared`.

## 2. Program.cs

```csharp
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthHttpMessageHandler>();

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/");
}).AddHttpMessageHandler<AuthHttpMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<ITicketApiClient, TicketApiClient>();
builder.Services.AddScoped<IDashboardApiClient, DashboardApiClient>();
```

## 3. AuthStateService

Odpowiada za:

- zapis access tokena,
- zapis refresh tokena,
- usuwanie tokenów przy logout,
- udostępnienie tokena handlerowi HTTP.

Docelowo tokeny można trzymać w `localStorage` dla demo albo w bezpieczniejszym wariancie wykorzystać BFF/cookie. W tej wersji zostajemy przy Blazor WASM + JWT.

## 4. AuthHttpMessageHandler

Każdy request do API dostaje nagłówek:

```text
Authorization: Bearer <access_token>
```

## 5. API clients

Docelowe klienty:

```text
IAuthApiClient
ITicketApiClient
IDashboardApiClient
INotificationApiClient
IAuditApiClient
```

## 6. Strony UI

```text
Pages/Auth/Login.razor
Pages/Auth/Logout.razor
Pages/Dashboard/Dashboard.razor
Pages/Tickets/Tickets.razor
Pages/Tickets/TicketDetails.razor
Pages/Tickets/TicketEdit.razor
Pages/Tickets/TicketCreate.razor
Pages/Tickets/DeletedTickets.razor
Pages/Admin/AuditLog.razor
```

## 7. Obsługa concurrency w UI

Jeżeli API zwróci `409 Conflict`, UI powinno pokazać komunikat:

```text
Ten ticket został zmieniony przez innego użytkownika. Odśwież dane i spróbuj ponownie.
```

## 8. Role w UI

UI może ukrywać przyciski, ale bezpieczeństwo wymusza API.

```razor
<AuthorizeView Roles="Admin,Agent">
    <button>Create ticket</button>
</AuthorizeView>
```

## 9. Live updates

UI łączy się z:

```text
/hubs/tickets
```

Po odebraniu `ticketListChanged` odświeża listę lub pokazuje toast.
