# 03 - Identity, JWT i refresh token per device

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. Założenie

Nie używamy ręcznego logowania. Produkcyjny kierunek:

- `ApplicationUser : IdentityUser`,
- role `Admin`, `Agent`, `User`,
- JWT jako access token,
- refresh token przechowywany w bazie jako hash,
- rotacja refresh tokena przy każdym odświeżeniu,
- możliwość wylogowania ze wszystkich urządzeń.

## 2. DTO

```csharp
public sealed class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Unknown";
}

public sealed class RefreshRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Unknown";
}

public sealed class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
}
```

## 3. TokenService

```csharp
public interface ITokenService
{
    Task<string> CreateAccessTokenAsync(ApplicationUser user, CancellationToken ct = default);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
```

Implementacja powinna:

- dodać claim `sub`, `nameid`, `unique_name`, role,
- opcjonalnie dodać claim `tenant_id`,
- podpisać token `HmacSha256`,
- generować refresh token z `RandomNumberGenerator.GetBytes(64)`,
- hashować refresh token przez SHA256.

## 4. AuthController - endpointy

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/revoke-all
```

### Login

1. Znajdź użytkownika przez `UserManager`.
2. Sprawdź hasło przez `SignInManager.CheckPasswordSignInAsync`.
3. Wygeneruj access token.
4. Wygeneruj refresh token.
5. Zapisz hash refresh tokena w `RefreshTokens`.
6. Zwróć `TokenResponseDto`.

### Refresh

1. Przyjmij refresh token.
2. Zahashuj go.
3. Znajdź aktywny token w bazie.
4. Oznacz stary token jako revoked.
5. Utwórz nowy refresh token.
6. Zwróć nowy access token i refresh token.

## 5. Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageTickets", p => p.RequireRole("Admin", "Agent"));
    options.AddPolicy("CanViewAudit", p => p.RequireRole("Admin"));
    options.AddPolicy("CanManageTenants", p => p.RequireRole("Admin"));
});
```

## 6. Seed ról i admina

```csharp
foreach (var role in new[] { "Admin", "Agent", "User" })
{
    if (!await roleManager.RoleExistsAsync(role))
        await roleManager.CreateAsync(new IdentityRole(role));
}
```

Dev admin:

```text
login: admin
hasło: Admin123!
rola: Admin
```

Wersja produkcyjna: hasła i użytkownicy startowi nie powinny być zaszyte w repozytorium.
