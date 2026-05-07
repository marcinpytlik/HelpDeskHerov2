# 17 — Backup, restore i runbook administracyjny

## Cel

Backup bez testu restore to tylko nadzieja, nie strategia.

## Założenia

```sql
ALTER DATABASE HelpDeskHeroDb SET RECOVERY FULL;
```

Przykładowe RPO/RTO:

```text
RPO: 15 minut
RTO: 60 minut
```

## Strategia

```text
FULL backup: codziennie
DIFF backup: co 4 godziny
LOG backup: co 15 minut
CHECKDB: codziennie lub według okna
Test restore: minimum raz w tygodniu
```

## FULL backup

```sql
BACKUP DATABASE HelpDeskHeroDb
TO DISK = 'X:\Backup\HelpDeskHeroDb_FULL.bak'
WITH COMPRESSION, CHECKSUM, STATS = 10;
```

## DIFF backup

```sql
BACKUP DATABASE HelpDeskHeroDb
TO DISK = 'X:\Backup\HelpDeskHeroDb_DIFF.bak'
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, STATS = 10;
```

## LOG backup

```sql
BACKUP LOG HelpDeskHeroDb
TO DISK = 'X:\Backup\HelpDeskHeroDb_LOG.trn'
WITH COMPRESSION, CHECKSUM, STATS = 10;
```

## VERIFYONLY

```sql
RESTORE VERIFYONLY
FROM DISK = 'X:\Backup\HelpDeskHeroDb_FULL.bak'
WITH CHECKSUM;
```

## CHECKDB

```sql
DBCC CHECKDB ('HelpDeskHeroDb') WITH NO_INFOMSGS, ALL_ERRORMSGS;
```

## Restore testowy

```sql
RESTORE DATABASE HelpDeskHeroDb_RestoreTest
FROM DISK = 'X:\Backup\HelpDeskHeroDb_FULL.bak'
WITH
    MOVE 'HelpDeskHeroDb' TO 'E:\SQL\Data\HelpDeskHeroDb_RestoreTest.mdf',
    MOVE 'HelpDeskHeroDb_log' TO 'F:\SQLLog\HelpDeskHeroDb_RestoreTest_log.ldf',
    NORECOVERY,
    CHECKSUM,
    STATS = 10;
```

## Point-in-time restore

```sql
RESTORE LOG HelpDeskHeroDb_RestoreTest
FROM DISK = 'X:\Backup\HelpDeskHeroDb_LOG.trn'
WITH
    STOPAT = '2026-04-25T02:12:30',
    RECOVERY,
    CHECKSUM,
    STATS = 10;
```

## Tail-log backup

```sql
BACKUP LOG HelpDeskHeroDb
TO DISK = 'X:\Backup\HelpDeskHeroDb_TAIL.trn'
WITH NO_TRUNCATE, COMPRESSION, CHECKSUM, STATS = 10;
```

## Runbook awaryjny

```text
1. Diagnoza: online/offline, log dostępny, punkt odtworzenia.
2. Zabezpieczenie: maintenance mode, tail-log backup, zabezpieczenie backupów.
3. Restore: FULL, DIFF, LOG, RECOVERY.
4. Weryfikacja: CHECKDB, dane krytyczne, loginy, aplikacja, joby.
```

## Checklist

- [ ] Czy baza jest w FULL recovery model?
- [ ] Czy FULL/DIFF/LOG backup działa?
- [ ] Czy backup ma CHECKSUM?
- [ ] Czy wykonujemy RESTORE VERIFYONLY?
- [ ] Czy wykonujemy test restore?
- [ ] Czy mamy procedurę point-in-time restore?
- [ ] Czy RPO/RTO są zaakceptowane?
