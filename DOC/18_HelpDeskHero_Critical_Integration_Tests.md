# 18 — Testy integracyjne krytycznych scenariuszy

## Cel

Testujemy nie tylko happy path, ale też bezpieczeństwo i blokady.

## Kategorie

```text
Auth
Permissions
Tenant isolation
Tickets
Workflow
Concurrency
Soft delete
Audit
Outbox
Files
Rate limiting
API error standard
```

## Auth

```text
Auth_Login_ReturnsAccessAndRefreshToken
Auth_Login_InvalidPassword_Returns401
Auth_RefreshToken_RotatesRefreshToken
Auth_RevokeSession_BlocksRefresh
Auth_ExpiredAccessToken_Returns401
```

## Permissions

```text
Permissions_UserWithoutTicketsRead_Gets403
Permissions_UserWithoutExport_Gets403
Permissions_AuditorCanReadAudit
Permissions_RequesterCannotManageWorkflow
```

## Tenant isolation

```text
Tenant_UserCannotReadTicketFromOtherTenant
Tenant_UserCannotUpdateTicketFromOtherTenant
Tenant_UserCannotDownloadAttachmentFromOtherTenant
Tenant_UserCannotSeeAuditFromOtherTenant
Tenant_CreateTicket_AssignsTenantFromClaims
```

Przykład:

```csharp
[Fact]
public async Task Tenant_UserCannotReadTicketFromOtherTenant()
{
    var tenantAClient = await _factory.CreateClientForTenantAsync("tenant-a");
    var ticketFromTenantB = await _factory.SeedTicketAsync("tenant-b");

    var response = await tenantAClient.GetAsync($"/api/tickets/{ticketFromTenantB.Id}");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

## Workflow

```text
Workflow_CreateTicket_SetsInitialState
Workflow_ValidTransition_ReturnsNoContent
Workflow_InvalidTransition_Returns409
Workflow_Transition_WritesAuditLog
Workflow_Transition_WritesOutboxMessage
```

## Concurrency

```text
Tickets_Update_WithCurrentRowVersion_ReturnsNoContent
Tickets_Update_WithOldRowVersion_Returns409
Tickets_Update_ConcurrencyConflict_ReturnsApiError
```

## Soft delete

```text
Tickets_SoftDelete_HidesFromDefaultList
Tickets_Restore_BringsBackTicket
Tickets_Delete_WritesAuditLog
```

## Audit i Outbox

```text
Audit_CreateTicket_WritesAuditLog
Audit_AuditLogContainsTenantId
Outbox_CreateTicket_WritesOutboxMessage
Outbox_Processor_MarksMessageAsProcessed
```

## Files

```text
Files_Upload_AllowedExtension_ReturnsCreated
Files_Upload_BlockedExtension_Returns400
Files_Upload_TooLarge_Returns400
Files_Download_DifferentTenant_Returns404
Files_Download_PendingScan_Returns409
```

## Rate limiting

```text
RateLimit_Login_Returns429AfterLimit
RateLimit_Export_Returns429AfterLimit
RateLimit_Upload_Returns429AfterLimit
```

## Minimalny zestaw krytyczny

```text
Auth_Login_ReturnsToken
Permissions_UserWithoutPermissionGets403
Tenant_UserCannotReadTicketFromOtherTenant
Tickets_Create_AssignsTenantFromClaims
Workflow_InvalidTransition_Returns409
Tickets_Update_WithOldRowVersion_Returns409
Tickets_SoftDelete_HidesFromDefaultList
Audit_CreateTicket_WritesAuditLog
Outbox_CreateTicket_WritesOutboxMessage
Files_Upload_BlocksForbiddenExtension
RateLimit_Login_Returns429AfterLimit
```

## Checklist

- [ ] Czy testy sprawdzają brak uprawnień?
- [ ] Czy testy sprawdzają cross-tenant access?
- [ ] Czy testy sprawdzają concurrency?
- [ ] Czy testy sprawdzają workflow?
- [ ] Czy testy sprawdzają audyt?
- [ ] Czy testy sprawdzają outbox?
- [ ] Czy testy sprawdzają upload plików?
- [ ] Czy testy sprawdzają rate limiting?
