# Odcinek 04 — bezpieczne migracje EF Core

Ten katalog zawiera pliki używane w odcinku 04:

- `00_create_database_and_security.sql` — skrypt dla administratora / DBA:
  - tworzy bazę `HelpDeskHeroDb`,
  - tworzy loginy `hdh_migrator` oraz `hdh_app`,
  - tworzy użytkowników w bazie,
  - nadaje `db_owner` tylko kontu migracyjnemu,
  - tworzy rolę `role_helpdeskhero_app`,
  - nadaje aplikacji runtime uprawnienia DML na schemat `dbo`.

- `migrations/01_generate_idempotent_migration_script.ps1` — komenda generująca idempotentny skrypt migracji EF Core.

Model bezpieczeństwa:

```text
Administrator / DBA  -> tworzy bazę, loginy, użytkowników i role
Konto migracyjne     -> wykonuje migracje EF Core i seed
Konto runtime        -> działa w aplikacji na minimalnych uprawnieniach
```

Ważne: hasła w plikach są demonstracyjne. W środowisku produkcyjnym użyj zmiennych środowiskowych, sejfu haseł albo innego mechanizmu zgodnego ze standardem organizacji.
