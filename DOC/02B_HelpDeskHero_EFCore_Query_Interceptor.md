# HelpDeskHero — EF Core Query Interceptor
## Logowanie zapytań SQL i pomiar czasu wykonania

Ten rozdział uzupełnia produkcyjny model EF Core o interceptor, który:

- loguje wykonywane komendy SQL,
- mierzy czas wykonania zapytań,
- oznacza wolne zapytania jako ostrzeżenia,
- pozwala powiązać zapytanie z tenantem i użytkownikiem,
- nie wymaga dopisywania logowania w każdym repozytorium, serwisie lub kontrolerze.

To rozwiązanie pasuje do architektury:

```text
Controller -> Application Service -> AppDbContext -> EF Core Interceptor -> SQL Server
```

Kontrolery i serwisy nie mierzą ręcznie czasu zapytań. Robi to warstwa infrastruktury EF Core.

---

## 1. Gdzie umieszczamy interceptor

Docelowa lokalizacja:

```text
src
└── HelpDeskHero.Api
    ├── Infrastructure
    │   ├── Persistence
    │   │   ├── AppDbContext.cs
    │   │   └── Interceptors
    │   │       └── EfQueryPerformanceInterceptor.cs
    │   └── Diagnostics
    │       └── QueryLogOptions.cs
    └── Program.cs
```

---

## 2. Opcje konfiguracyjne

### Plik

`src/HelpDeskHero.Api/Infrastructure/Diagnostics/QueryLogOptions.cs`

```csharp
namespace HelpDeskHero.Api.Infrastructure.Diagnostics;

public sealed class QueryLogOptions
{
    public int SlowQueryThresholdMilliseconds { get; set; } = 500;
    public bool LogSqlParameters { get; set; } = false;
    public bool LogSqlText { get; set; } = true;
    public int MaxSqlTextLength { get; set; } = 4000;
}
```

### Konfiguracja

`appsettings.Development.json`

```json
{
  "Diagnostics": {
    "QueryLogging": {
      "SlowQueryThresholdMilliseconds": 500,
      "LogSqlParameters": false,
      "LogSqlText": true,
      "MaxSqlTextLength": 4000
    }
  }
}
```

W produkcji zwykle zostawiłbym:

```json
{
  "Diagnostics": {
    "QueryLogging": {
      "SlowQueryThresholdMilliseconds": 1000,
      "LogSqlParameters": false,
      "LogSqlText": false,
      "MaxSqlTextLength": 2000
    }
  }
}
```

---

## 3. Interceptor EF Core

### Plik

`src/HelpDeskHero.Api/Infrastructure/Persistence/Interceptors/EfQueryPerformanceInterceptor.cs`

```csharp
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using HelpDeskHero.Api.Application.Common;
using HelpDeskHero.Api.Infrastructure.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace HelpDeskHero.Api.Infrastructure.Persistence.Interceptors;

public sealed class EfQueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<EfQueryPerformanceInterceptor> _logger;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly QueryLogOptions _options;
    private readonly ConcurrentDictionary<Guid, Stopwatch> _timers = new();

    public EfQueryPerformanceInterceptor(
        ILogger<EfQueryPerformanceInterceptor> logger,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        IOptions<QueryLogOptions> options)
    {
        _logger = logger;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _options = options.Value;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        StartTimer(eventData.CommandId);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        StopTimerAndLog(command, eventData, "Reader");
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        StartTimer(eventData.CommandId);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        StopTimerAndLog(command, eventData, "ReaderAsync");
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        StartTimer(eventData.CommandId);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        StopTimerAndLog(command, eventData, "NonQuery");
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StartTimer(eventData.CommandId);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        StopTimerAndLog(command, eventData, "NonQueryAsync");
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        StartTimer(eventData.CommandId);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override object ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result)
    {
        StopTimerAndLog(command, eventData, "Scalar");
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        StartTimer(eventData.CommandId);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        StopTimerAndLog(command, eventData, "ScalarAsync");
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        StopTimerAndLogFailure(command, eventData);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        StopTimerAndLogFailure(command, eventData);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private void StartTimer(Guid commandId)
    {
        _timers[commandId] = Stopwatch.StartNew();
    }

    private void StopTimerAndLog(
        DbCommand command,
        CommandExecutedEventData eventData,
        string operation)
    {
        var elapsedMs = StopTimer(eventData.CommandId);
        var sqlHash = CreateSqlHash(command.CommandText);
        var sqlText = BuildSqlText(command);
        var parameters = BuildParametersText(command);
        var tenantId = SafeTenantId();
        var userId = _currentUser.UserId ?? "anonymous";

        if (elapsedMs >= _options.SlowQueryThresholdMilliseconds)
        {
            _logger.LogWarning(
                "Slow EF Core query detected. Operation={Operation}; ElapsedMs={ElapsedMs}; ThresholdMs={ThresholdMs}; TenantId={TenantId}; UserId={UserId}; CommandId={CommandId}; SqlHash={SqlHash}; Sql={Sql}; Parameters={Parameters}",
                operation,
                elapsedMs,
                _options.SlowQueryThresholdMilliseconds,
                tenantId,
                userId,
                eventData.CommandId,
                sqlHash,
                sqlText,
                parameters);
        }
        else
        {
            _logger.LogInformation(
                "EF Core query executed. Operation={Operation}; ElapsedMs={ElapsedMs}; TenantId={TenantId}; UserId={UserId}; CommandId={CommandId}; SqlHash={SqlHash}; Sql={Sql}; Parameters={Parameters}",
                operation,
                elapsedMs,
                tenantId,
                userId,
                eventData.CommandId,
                sqlHash,
                sqlText,
                parameters);
        }
    }

    private void StopTimerAndLogFailure(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        var elapsedMs = StopTimer(eventData.CommandId);
        var sqlHash = CreateSqlHash(command.CommandText);
        var sqlText = BuildSqlText(command);
        var parameters = BuildParametersText(command);
        var tenantId = SafeTenantId();
        var userId = _currentUser.UserId ?? "anonymous";

        _logger.LogError(
            eventData.Exception,
            "EF Core query failed. ElapsedMs={ElapsedMs}; TenantId={TenantId}; UserId={UserId}; CommandId={CommandId}; SqlHash={SqlHash}; Sql={Sql}; Parameters={Parameters}",
            elapsedMs,
            tenantId,
            userId,
            eventData.CommandId,
            sqlHash,
            sqlText,
            parameters);
    }

    private long StopTimer(Guid commandId)
    {
        if (!_timers.TryRemove(commandId, out var stopwatch))
            return -1;

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    private string SafeTenantId()
    {
        try
        {
            return _tenantProvider.GetTenantId().ToString();
        }
        catch
        {
            return "unknown";
        }
    }

    private string BuildSqlText(DbCommand command)
    {
        if (!_options.LogSqlText)
            return "disabled";

        var sql = NormalizeSql(command.CommandText);

        if (sql.Length <= _options.MaxSqlTextLength)
            return sql;

        return sql[.._options.MaxSqlTextLength] + "... [truncated]";
    }

    private string BuildParametersText(DbCommand command)
    {
        if (!_options.LogSqlParameters)
            return "disabled";

        if (command.Parameters.Count == 0)
            return "none";

        var sb = new StringBuilder();

        foreach (DbParameter parameter in command.Parameters)
        {
            sb.Append(parameter.ParameterName);
            sb.Append('=');
            sb.Append(parameter.Value ?? "NULL");
            sb.Append("; ");
        }

        return sb.ToString();
    }

    private static string NormalizeSql(string sql)
    {
        return string.Join(' ', sql.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CreateSqlHash(string sql)
    {
        var normalizedSql = NormalizeSql(sql);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedSql));
        return Convert.ToHexString(bytes)[..16];
    }
}
```

---

## 4. Minimalne kontrakty wspólne

Interceptor korzysta z `ICurrentUserService` i `ITenantProvider`, które są już częścią warstwy aplikacyjnej.
Jeżeli ich jeszcze nie ma w kodzie, dodaj minimalne kontrakty.

### `Application/Common/ICurrentUserService.cs`

```csharp
namespace HelpDeskHero.Api.Application.Common;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
```

### `Application/Common/ITenantProvider.cs`

```csharp
namespace HelpDeskHero.Api.Application.Common;

public interface ITenantProvider
{
    Guid GetTenantId();
    string? GetTenantCode();
}
```

---

## 5. Rejestracja w DI

### `Program.cs`

```csharp
using HelpDeskHero.Api.Infrastructure.Diagnostics;
using HelpDeskHero.Api.Infrastructure.Persistence.Interceptors;

builder.Services.Configure<QueryLogOptions>(
    builder.Configuration.GetSection("Diagnostics:QueryLogging"));

builder.Services.AddScoped<EfQueryPerformanceInterceptor>();
```

---

## 6. Podpięcie interceptora do DbContext

### `Program.cs`

```csharp
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var queryInterceptor = serviceProvider.GetRequiredService<EfQueryPerformanceInterceptor>();

    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(queryInterceptor);
});
```

Ważne: używamy wariantu `AddDbContext((serviceProvider, options) => ...)`, bo interceptor ma zależności z DI.

---

## 7. Konfiguracja logowania

### `appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "HelpDeskHero.Api.Infrastructure.Persistence.Interceptors.EfQueryPerformanceInterceptor": "Information"
    }
  }
}
```

Ustawienie `Microsoft.EntityFrameworkCore.Database.Command` na `Warning` ogranicza standardowe logi EF Core, a własny interceptor przejmuje kontrolowane logowanie.

---

## 8. Dlaczego nie tylko `LogTo`?

EF Core ma wbudowane logowanie przez `LogTo`, ale interceptor daje większą kontrolę:

- możesz mierzyć czas per komenda,
- możesz ustawić próg slow query,
- możesz dodać `TenantId`, `UserId`, `CommandId`, `SqlHash`,
- możesz osobno logować błędy,
- możesz w przyszłości zapisywać metryki do tabeli, Prometheus, OpenTelemetry albo Application Insights.

`LogTo` jest dobre do szybkiej diagnostyki developerskiej. Interceptor jest lepszy jako element architektury produkcyjnej.

---

## 9. Uwaga o parametrach SQL

Domyślnie:

```json
"LogSqlParameters": false
```

To jest celowe. Parametry mogą zawierać dane użytkowników, tytuły zgłoszeń, adresy email, treść komentarzy albo inne dane wrażliwe.

Do lokalnego debugowania można tymczasowo włączyć:

```json
"LogSqlParameters": true
```

Nie zalecam tego jako ustawienia produkcyjnego.

---

## 10. Przykładowy log

```text
EF Core query executed. Operation=ReaderAsync; ElapsedMs=42; TenantId=2f9c2c4a-6b6f-4b8e-9e47-7e98f6c6c001; UserId=8b1147f0; CommandId=87fb1d24-bc12-41e2-b6c1-bf7c39d76fd2; SqlHash=AB12CD34EF56AA90; Sql=SELECT [t].[Id], [t].[Number], [t].[Title] FROM [Tickets] AS [t] WHERE [t].[TenantId] = @__tenantId_0; Parameters=disabled
```

Przykład slow query:

```text
Slow EF Core query detected. Operation=ReaderAsync; ElapsedMs=1420; ThresholdMs=1000; TenantId=2f9c2c4a-6b6f-4b8e-9e47-7e98f6c6c001; UserId=8b1147f0; CommandId=87fb1d24-bc12-41e2-b6c1-bf7c39d76fd2; SqlHash=AB12CD34EF56AA90; Sql=SELECT ...; Parameters=disabled
```

---

## 11. Rozszerzenie na zapis do tabeli diagnostycznej

Na start logowanie przez `ILogger` wystarczy. Jeżeli chcesz potem trzymać historię w SQL Server, można dodać tabelę:

```text
EfQueryLogs
```

Przykładowe pola:

```csharp
public sealed class EfQueryLog
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
    public bool IsSlow { get; set; }
    public string SqlHash { get; set; } = string.Empty;
    public string? SqlText { get; set; }
    public string? Error { get; set; }
}
```

Ale nie zapisuj tego przez ten sam `AppDbContext` w interceptorze, bo łatwo zrobić pętlę:

```text
Interceptor -> zapis logu EF -> interceptor -> zapis logu EF -> ...
```

Do trwałego zapisu użyj osobnego mechanizmu:

- `ILogger` + Serilog sink do SQL Server,
- kanał `Channel<T>` + `BackgroundService`,
- osobny `DbConnection` bez EF Core,
- OpenTelemetry.

---

## 12. Checklist wdrożeniowy

- [ ] Dodać `QueryLogOptions`
- [ ] Dodać `EfQueryPerformanceInterceptor`
- [ ] Zarejestrować `QueryLogOptions` w DI
- [ ] Zarejestrować `EfQueryPerformanceInterceptor` jako `Scoped`
- [ ] Podpiąć interceptor w `AddDbContext`
- [ ] Ustawić próg slow query dla dev i prod
- [ ] Zostawić `LogSqlParameters=false` w produkcji
- [ ] Sprawdzić logi dla `GetTickets`, `GetTicketDetails`, `CreateTicket`, `UpdateTicket`
- [ ] Dodać dashboard/metryki slow query jako osobny etap, jeżeli będzie potrzebne

---

## 13. Decyzja architektoniczna

Dla HelpDeskHero przyjmujemy:

```text
EF Core interceptor jest częścią Infrastructure/Persistence.
```

Nie wkładamy pomiaru czasu zapytań do:

```text
Controllers
Application Services
Domain Entities
DTO
```

Dzięki temu logowanie SQL jest przekrojowe, spójne i nie zanieczyszcza przypadków użycia.
