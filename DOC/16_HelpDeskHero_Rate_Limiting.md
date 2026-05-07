# 16 — Rate limiting

## Cel

Rate limiting chroni API przed brute force, nadużyciem uploadu, eksportu i kosztownych endpointów.

## Endpointy

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/tickets/{id}/attachments
GET  /api/tickets/export
GET  /api/tickets
POST /api/webhooks/incoming
```

## Rejestracja

```csharp
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("upload", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(10);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("export", limiter =>
    {
        limiter.PermitLimit = 3;
        limiter.Window = TimeSpan.FromMinutes(10);
        limiter.QueueLimit = 0;
    });
});
```

Pipeline:

```csharp
app.UseRateLimiter();
```

## Użycie

```csharp
[EnableRateLimiting("login")]
[HttpPost("login")]
public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto dto)
{
    // login
}
```

## Standard błędu 429

```json
{
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Too many requests.",
  "traceId": "..."
}
```

## Checklist

- [ ] Czy login ma rate limit?
- [ ] Czy refresh token ma rate limit?
- [ ] Czy upload ma rate limit?
- [ ] Czy export ma rate limit?
- [ ] Czy 429 zwraca standardowy model błędu?
- [ ] Czy test integracyjny sprawdza przekroczenie limitu?
