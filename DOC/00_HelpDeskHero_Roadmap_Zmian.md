# 00 - Roadmap zmian do wersji produkcyjnej

> Wersja: **HelpDeskHero Production Edition**
> Stack: **.NET 10, ASP.NET Core Web API, EF Core, SQL Server 2022, Blazor WebAssembly, ASP.NET Identity, JWT, Hangfire, SignalR, xUnit, bUnit, GitHub Actions**.


## 1. Dlaczego przebudowa

Poprzednia wersja była dobra jako ścieżka edukacyjna, ale produkcyjnie miała problem: fundamenty były kilka razy wymieniane. Najpierw prosty backend w pamięci, później EF Core, potem ręczny JWT, później refresh tokeny, a dopiero następnie ASP.NET Identity.

W wersji produkcyjnej nie robimy tego etapowo. Projekt startuje od docelowych decyzji.

## 2. Najważniejsze decyzje

| Obszar | Nowa decyzja |
|---|---|
| Auth | ASP.NET Identity od początku |
| Access token | JWT Bearer |
| Refresh token | per device / per session, przechowywany jako hash |
| Role | `Admin`, `Agent`, `User` |
| Autoryzacja | policies: `CanManageTickets`, `CanViewAudit`, `CanManageTenants` |
| ORM | EF Core + SQL Server |
| Concurrency | `RowVersion` na encji `Ticket` |
| Kasowanie | soft delete + restore |
| Audit | osobna tabela `AuditLogs` |
| Procesy w tle | Hangfire + SQL Server storage |
| Live updates | SignalR |
| Integracje | outbox pattern |
| Multi-tenant | wspólna baza + `TenantId` + global query filters |

## 3. Co usuwamy jako wariant docelowy

- ręczny kontroler logowania z hasłami typu `admin/Admin123!`,
- osobną encję użytkownika niezależną od Identity,
- kilka dublujących się DTO do tokenów,
- kilka wersji `Ticket`,
- logikę biznesową w kontrolerach,
- wysyłkę powiadomień bezpośrednio z requestu HTTP.

## 4. Co zostaje z poprzednich plików

Zostają wszystkie dobre pomysły, ale w nowym porządku: API + UI + Shared, EF Core i SQL Server, JWT, refresh tokeny, role i policies, soft delete, optimistic concurrency, audit log, dashboard, export CSV, testy, Hangfire, komentarze i załączniki, SignalR, SLA, outbox, multi-tenant, workflow, knowledge base, webhooks/RabbitMQ.

## 5. Kolejność implementacji

1. Solution i struktura projektów.
2. Shared contracts.
3. Domain model i DbContext.
4. Identity + JWT + refresh token.
5. Ticket CRUD z soft delete i concurrency.
6. Audit log i middleware błędów.
7. Blazor WASM + API clients.
8. Dashboard i CSV export.
9. Testy integracyjne i bUnit.
10. Hangfire, komentarze, załączniki.
11. SignalR, SLA, outbox.
12. Multi-tenant, workflow, KB, webhooks.

## 6. Kryterium sukcesu

- `dotnet build` przechodzi dla całego solution.
- `dotnet test` przechodzi.
- API startuje i tworzy schemat bazy.
- Login przez Identity działa.
- Refresh token działa i rotuje tokeny.
- Ticket CRUD działa.
- Soft delete ukrywa rekordy.
- Restore przywraca rekordy.
- RowVersion zwraca `409 Conflict` przy konflikcie.
- Audit zapisuje operacje.
- Hangfire i SignalR są osobnymi modułami, nie logiką w kontrolerze.


## Aktualizacja v3 - Application Services i cienkie kontrolery

W poprzedniej paczce była tylko ogólna zasada, że kontrolery są cienkie. Brakowało jednak osobnego, praktycznego rozdziału z warstwą `Application`, interfejsami serwisów oraz przykładem lekkiego kontrolera.

Dodano plik:

```text
03A_HelpDeskHero_Application_Services_Thin_Controllers.md
```

Od tej wersji obowiązuje zasada:

```text
Controllers -> Application Services -> Domain + Infrastructure
```

Kontrolery nie powinny bezpośrednio orkiestracyjnie używać `AppDbContext`, audytu, outboxa, Hangfire, SignalR ani obsługi plików.

## Produkcyjny seed danych

Do dokumentacji dodano standard seedowania danych startowych w osobnych klasach `ISeedStep`.

```text
Infrastructure/Persistence/Seeding
├── ISeedStep
├── IDatabaseSeeder
├── DatabaseSeeder
├── IdentitySeedStep
├── TenantSeedStep
├── OrganizationUnitSeedStep
├── TicketTypeSeedStep
├── WorkflowSeedStep
├── TicketSlaPolicySeedStep
├── KnowledgeBaseSeedStep
├── WebhookSubscriptionSeedStep
└── KpiSeedStep
```

Seed jest idempotentny. W produkcji uruchamia go konto migracyjne `hdh_migrator`, a nie konto runtime aplikacji `hdh_app`.

Szczegóły: `02E_HelpDeskHero_EFCore_Production_Seed_Data.md`.
