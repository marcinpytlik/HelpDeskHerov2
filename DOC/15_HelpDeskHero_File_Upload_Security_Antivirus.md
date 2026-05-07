# 15 — Bezpieczeństwo uploadu plików i wariant antywirusowy

## Cel

Upload plików wymaga osobnej polityki bezpieczeństwa.

## Zasady

```text
Nie zapisujemy plików pod oryginalną nazwą fizyczną.
Nie przechowujemy uploadów w wwwroot.
Nie pozwalamy pobierać pliku bez autoryzacji.
Nie ufamy Content-Type.
Nie ufamy rozszerzeniu.
Blokujemy typy wykonywalne.
Ustawiamy limit rozmiaru.
Logujemy upload/download/delete w audycie.
W wariancie AV plik jest dostępny dopiero po statusie Clean.
```

## Blokowane rozszerzenia

```text
.exe
.bat
.cmd
.ps1
.vbs
.js
.msi
.dll
.com
.scr
.pif
.reg
```

## Model

```csharp
public enum AttachmentScanStatus
{
    NotRequired = 0,
    Pending = 1,
    Clean = 2,
    Infected = 3,
    Failed = 4
}
```

```csharp
public sealed class TicketAttachment
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public int TicketId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public AttachmentScanStatus ScanStatus { get; set; } = AttachmentScanStatus.Pending;
    public string? ScanResult { get; set; }
    public DateTime? ScannedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
}
```

## Bezpieczna nazwa

```csharp
public static string GenerateStoredFileName(string originalFileName)
{
    var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
    return $"{Guid.NewGuid():N}{extension}";
}
```

## Przepływ z AV

```text
Upload -> PendingScan
Background Job -> skan AV
Clean -> dostępny do pobrania
Infected -> zablokowany
```

## Checklist

- [ ] Czy upload ma limit rozmiaru?
- [ ] Czy blokujemy niebezpieczne rozszerzenia?
- [ ] Czy pliki są poza wwwroot?
- [ ] Czy nazwa fizyczna jest generowana?
- [ ] Czy pobieranie sprawdza TenantId?
- [ ] Czy upload/download/delete trafia do audytu?
- [ ] Czy skan AV blokuje plik do czasu statusu Clean?
