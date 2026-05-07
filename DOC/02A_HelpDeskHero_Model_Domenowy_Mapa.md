# 02A - Mapa modelu domenowego HelpDeskHero Production

Ten plik jest szybką mapą encji po uzupełnieniu modelu produkcyjnego.

## Encje dodane do modelu

```text
Tenant
OrganizationUnit
TicketType
WorkflowDefinition
WorkflowState
WorkflowTransition
KnowledgeArticle
WebhookSubscription
KpiSnapshot
```

## Relacje główne

```text
Tenant 1..n OrganizationUnit
Tenant 1..n ApplicationUser
Tenant 1..n TicketType
Tenant 1..n Ticket
Tenant 1..n KnowledgeArticle
Tenant 1..n WebhookSubscription
Tenant 1..n KpiSnapshot

TicketType 1..n WorkflowDefinition
WorkflowDefinition 1..n WorkflowState
WorkflowDefinition 1..n WorkflowTransition
WorkflowTransition n..1 WorkflowState jako FromState
WorkflowTransition n..1 WorkflowState jako ToState

Ticket n..1 Tenant
Ticket n..1 TicketType
Ticket n..1 WorkflowState
Ticket 1..n TicketComment
Ticket 1..n TicketAttachment
Ticket 1..n TicketEscalation
```

## Decyzja architektoniczna

Od tej wersji `Ticket.Status` nie jest głównym źródłem prawdy. Stan zgłoszenia wynika z `WorkflowStateId`.

W DTO można nadal wystawiać pole `Status`, ale powinno ono być mapowane z `WorkflowState.Code` albo `WorkflowState.Name`.
