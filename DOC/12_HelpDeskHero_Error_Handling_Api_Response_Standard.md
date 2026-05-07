# 12 — Obsługa błędów i standard odpowiedzi API

## Cel

Wszystkie błędy API powinny mieć jeden standardowy format.

## Model błędu

```csharp
public sealed class ApiErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
}
```

## Kody błędów

```text
VALIDATION_ERROR
NOT_FOUND
UNAUTHORIZED
FORBIDDEN
CONFLICT
CONCURRENCY_CONFLICT
BUSINESS_RULE_VIOLATION
TENANT_ACCESS_DENIED
WORKFLOW_TRANSITION_NOT_ALLOWED
FILE_REJECTED
FILE_SCAN_FAILED
RATE_LIMIT_EXCEEDED
INTERNAL_ERROR
```

## AppException

```csharp
public abstract class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected AppException(string code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}
```

## Middleware

```csharp
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteErrorAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict.");
            await WriteErrorAsync(context, 409, "CONCURRENCY_CONFLICT",
                "The resource was modified by another user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteErrorAsync(context, 500, "INTERNAL_ERROR", "Unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Code = code,
            Message = message,
            TraceId = traceId
        });
    }
}
```

## Mapowanie

| Scenariusz | HTTP | Code |
|---|---:|---|
| Walidacja DTO | 400 | `VALIDATION_ERROR` |
| Brak logowania | 401 | `UNAUTHORIZED` |
| Brak uprawnień | 403 | `FORBIDDEN` |
| Brak rekordu | 404 | `NOT_FOUND` |
| Konflikt RowVersion | 409 | `CONCURRENCY_CONFLICT` |
| Niedozwolone workflow | 409 | `WORKFLOW_TRANSITION_NOT_ALLOWED` |
| Plik odrzucony | 400 | `FILE_REJECTED` |
| Rate limit | 429 | `RATE_LIMIT_EXCEEDED` |
| Błąd nieobsłużony | 500 | `INTERNAL_ERROR` |

## Checklist

- [ ] Czy wszystkie błędy API mają jeden format?
- [ ] Czy odpowiedź zawiera TraceId?
- [ ] Czy produkcja nie zwraca stack trace?
- [ ] Czy concurrency zwraca 409?
- [ ] Czy UI umie obsłużyć ApiErrorResponse?
