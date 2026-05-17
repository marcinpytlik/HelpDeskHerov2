IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE TABLE [OrganizationUnits] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [ParentOrganizationUnitId] int NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OrganizationUnits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationUnits_OrganizationUnits_ParentOrganizationUnitId] FOREIGN KEY ([ParentOrganizationUnitId]) REFERENCES [OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrganizationUnits_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE TABLE [TicketTypes] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TicketTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketTypes_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE TABLE [WorkflowDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [TicketTypeId] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkflowDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowDefinitions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowDefinitions_TicketTypes_TicketTypeId] FOREIGN KEY ([TicketTypeId]) REFERENCES [TicketTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE TABLE [WorkflowStates] (
        [Id] int NOT NULL IDENTITY,
        [WorkflowDefinitionId] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsStart] bit NOT NULL,
        [IsFinal] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkflowStates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowStates_WorkflowDefinitions_WorkflowDefinitionId] FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [WorkflowDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE TABLE [Tickets] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [OrganizationUnitId] int NULL,
        [TicketTypeId] int NOT NULL,
        [WorkflowStateId] int NOT NULL,
        [Number] nvarchar(50) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [Priority] nvarchar(50) NOT NULL,
        [CreatedByUserId] nvarchar(450) NOT NULL,
        [AssignedToUserId] nvarchar(450) NULL,
        [DueResponseAtUtc] datetime2 NULL,
        [DueResolveAtUtc] datetime2 NULL,
        [ResolvedAtUtc] datetime2 NULL,
        [ClosedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tickets_OrganizationUnits_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_TicketTypes_TicketTypeId] FOREIGN KEY ([TicketTypeId]) REFERENCES [TicketTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_WorkflowStates_WorkflowStateId] FOREIGN KEY ([WorkflowStateId]) REFERENCES [WorkflowStates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE TABLE [WorkflowTransitions] (
        [Id] int NOT NULL IDENTITY,
        [WorkflowDefinitionId] int NOT NULL,
        [FromStateId] int NOT NULL,
        [ToStateId] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [RequiresComment] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkflowTransitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowTransitions_WorkflowDefinitions_WorkflowDefinitionId] FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [WorkflowDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkflowTransitions_WorkflowStates_FromStateId] FOREIGN KEY ([FromStateId]) REFERENCES [WorkflowStates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowTransitions_WorkflowStates_ToStateId] FOREIGN KEY ([ToStateId]) REFERENCES [WorkflowStates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrganizationUnits_ParentOrganizationUnitId] ON [OrganizationUnits] ([ParentOrganizationUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrganizationUnits_TenantId_Code] ON [OrganizationUnits] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Code] ON [Tenants] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_OrganizationUnitId] ON [Tickets] ([OrganizationUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_TenantId_AssignedToUserId_CreatedAtUtc] ON [Tickets] ([TenantId], [AssignedToUserId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_TenantId_IsDeleted_CreatedAtUtc] ON [Tickets] ([TenantId], [IsDeleted], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tickets_TenantId_Number] ON [Tickets] ([TenantId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_TenantId_WorkflowStateId_CreatedAtUtc] ON [Tickets] ([TenantId], [WorkflowStateId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_TicketTypeId] ON [Tickets] ([TicketTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_WorkflowStateId] ON [Tickets] ([WorkflowStateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TicketTypes_TenantId_Code] ON [TicketTypes] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkflowDefinitions_TenantId_TicketTypeId_Code] ON [WorkflowDefinitions] ([TenantId], [TicketTypeId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowDefinitions_TicketTypeId] ON [WorkflowDefinitions] ([TicketTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkflowStates_WorkflowDefinitionId_Code] ON [WorkflowStates] ([WorkflowDefinitionId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowStates_WorkflowDefinitionId_SortOrder] ON [WorkflowStates] ([WorkflowDefinitionId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowTransitions_FromStateId] ON [WorkflowTransitions] ([FromStateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowTransitions_ToStateId] ON [WorkflowTransitions] ([ToStateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkflowTransitions_WorkflowDefinitionId_FromStateId_ToStateId] ON [WorkflowTransitions] ([WorkflowDefinitionId], [FromStateId], [ToStateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517205638_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260517205638_InitialCreate', N'10.0.7');
END;

COMMIT;
GO

