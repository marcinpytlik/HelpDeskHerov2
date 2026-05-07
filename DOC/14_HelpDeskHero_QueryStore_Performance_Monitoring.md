# 14 — Query Store i monitoring wydajności

## Cel

EF Core interceptor daje szybki obraz po stronie aplikacji. Query Store daje trwałą historię po stronie SQL Server.

## Włączenie Query Store

```sql
ALTER DATABASE HelpDeskHeroDb
SET QUERY_STORE = ON;
GO

ALTER DATABASE HelpDeskHeroDb
SET QUERY_STORE (
    OPERATION_MODE = READ_WRITE,
    QUERY_CAPTURE_MODE = AUTO,
    MAX_STORAGE_SIZE_MB = 1024,
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    INTERVAL_LENGTH_MINUTES = 60
);
GO
```

## Sprawdzenie konfiguracji

```sql
SELECT
    actual_state_desc,
    desired_state_desc,
    current_storage_size_mb,
    max_storage_size_mb,
    query_capture_mode_desc
FROM sys.database_query_store_options;
```

## Top queries by duration

```sql
SELECT TOP (25)
    qt.query_sql_text,
    q.query_id,
    p.plan_id,
    rs.count_executions,
    CAST(rs.avg_duration / 1000.0 AS decimal(18,2)) AS avg_duration_ms,
    CAST(rs.avg_cpu_time / 1000.0 AS decimal(18,2)) AS avg_cpu_ms,
    rs.avg_logical_io_reads
FROM sys.query_store_query_text AS qt
JOIN sys.query_store_query AS q
    ON qt.query_text_id = q.query_text_id
JOIN sys.query_store_plan AS p
    ON q.query_id = p.query_id
JOIN sys.query_store_runtime_stats AS rs
    ON p.plan_id = rs.plan_id
ORDER BY rs.avg_duration DESC;
```

## Baseline

Przed większą zmianą zapisujemy:

```text
top queries by duration
top queries by CPU
top queries by logical reads
queries with multiple plans
najcięższe endpointy
```

## Plan forcing

```sql
EXEC sys.sp_query_store_force_plan
    @query_id = 123,
    @plan_id = 456;
```

Usunięcie:

```sql
EXEC sys.sp_query_store_unforce_plan
    @query_id = 123,
    @plan_id = 456;
```

Plan forcing traktujemy jako działanie ostrożne i najlepiej tymczasowe.

## Checklist

- [ ] Czy Query Store jest włączony?
- [ ] Czy storage ma limit?
- [ ] Czy baseline jest zapisany przed dużą zmianą?
- [ ] Czy slow queries z aplikacji są porównywane z Query Store?
- [ ] Czy plan forcing jest używany ostrożnie?
