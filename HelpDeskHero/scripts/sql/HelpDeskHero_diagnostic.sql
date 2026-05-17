/* ============================================================
   HelpDeskHero — diagnostyka bazy, tabel, kont, ról i uprawnień
   Baza: HelpDeskHeroDb
   ============================================================ */

USE master;
GO

PRINT '=== 1. Czy baza istnieje? ===';

SELECT
    d.name AS DatabaseName,
    d.database_id,
    d.create_date,
    d.state_desc,
    d.recovery_model_desc,
    d.compatibility_level,
    d.collation_name
FROM sys.databases AS d
WHERE d.name = N'HelpDeskHeroDb';
GO


PRINT '=== 2. Loginy serwerowe związane z HelpDeskHero ===';

SELECT
    sp.name AS LoginName,
    sp.type_desc AS LoginType,
    sp.is_disabled,
    sp.create_date,
    sp.modify_date,
    sp.default_database_name
FROM sys.server_principals AS sp
WHERE sp.name IN
(
    N'hdh_migrator',
    N'hdh_app'
)
ORDER BY sp.name;
GO


USE HelpDeskHeroDb;
GO

PRINT '=== 3. Tabele w bazie HelpDeskHeroDb ===';

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    t.create_date,
    t.modify_date,
    p.rows AS ApproxRows
FROM sys.tables AS t
JOIN sys.schemas AS s
    ON t.schema_id = s.schema_id
LEFT JOIN sys.partitions AS p
    ON t.object_id = p.object_id
   AND p.index_id IN (0, 1)
ORDER BY
    s.name,
    t.name;
GO


PRINT '=== 4. Kolumny w tabelach ===';

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.column_id,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    c.is_identity,
    c.is_rowguidcol,
    c.is_computed
FROM sys.tables AS t
JOIN sys.schemas AS s
    ON t.schema_id = s.schema_id
JOIN sys.columns AS c
    ON t.object_id = c.object_id
JOIN sys.types AS ty
    ON c.user_type_id = ty.user_type_id
ORDER BY
    s.name,
    t.name,
    c.column_id;
GO


PRINT '=== 5. Użytkownicy w bazie ===';

SELECT
    dp.name AS DatabaseUserName,
    dp.type_desc AS UserType,
    dp.authentication_type_desc,
    dp.create_date,
    dp.modify_date,
    dp.default_schema_name
FROM sys.database_principals AS dp
WHERE dp.type IN ('S', 'U', 'G')
  AND dp.name NOT IN
  (
      N'dbo',
      N'guest',
      N'INFORMATION_SCHEMA',
      N'sys'
  )
ORDER BY dp.name;
GO


PRINT '=== 6. Role bazodanowe ===';

SELECT
    dp.name AS RoleName,
    dp.type_desc,
    dp.create_date,
    dp.modify_date
FROM sys.database_principals AS dp
WHERE dp.type = 'R'
ORDER BY dp.name;
GO


PRINT '=== 7. Członkostwo użytkowników w rolach ===';

SELECT
    role_principal.name AS RoleName,
    member_principal.name AS MemberName,
    member_principal.type_desc AS MemberType
FROM sys.database_role_members AS drm
JOIN sys.database_principals AS role_principal
    ON drm.role_principal_id = role_principal.principal_id
JOIN sys.database_principals AS member_principal
    ON drm.member_principal_id = member_principal.principal_id
ORDER BY
    role_principal.name,
    member_principal.name;
GO


PRINT '=== 8. Uprawnienia jawnie nadane w bazie ===';

SELECT
    pr.name AS PrincipalName,
    pr.type_desc AS PrincipalType,
    pe.state_desc,
    pe.permission_name,
    pe.class_desc,
    CASE pe.class
        WHEN 0 THEN DB_NAME()
        WHEN 1 THEN OBJECT_SCHEMA_NAME(pe.major_id) + N'.' + OBJECT_NAME(pe.major_id)
        WHEN 3 THEN SCHEMA_NAME(pe.major_id)
        ELSE CONVERT(nvarchar(50), pe.major_id)
    END AS SecurableName
FROM sys.database_permissions AS pe
JOIN sys.database_principals AS pr
    ON pe.grantee_principal_id = pr.principal_id
WHERE pr.name IN
(
    N'hdh_migrator',
    N'hdh_app',
    N'role_helpdeskhero_app'
)
ORDER BY
    pr.name,
    pe.class_desc,
    pe.permission_name;
GO


PRINT '=== 9. Klucze obce ===';

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ChildColumn,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ParentColumn,
    fk.delete_referential_action_desc AS OnDelete,
    fk.update_referential_action_desc AS OnUpdate,
    fk.is_disabled,
    fk.is_not_trusted
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc
    ON fk.object_id = fkc.constraint_object_id
ORDER BY
    ChildSchema,
    ChildTable,
    ForeignKeyName;
GO


PRINT '=== 10. Indeksy ===';

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc,
    i.is_unique,
    i.is_primary_key,
    i.is_unique_constraint,
    STRING_AGG(c.name, N', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS KeyColumns
FROM sys.indexes AS i
JOIN sys.tables AS t
    ON i.object_id = t.object_id
JOIN sys.schemas AS s
    ON t.schema_id = s.schema_id
JOIN sys.index_columns AS ic
    ON i.object_id = ic.object_id
   AND i.index_id = ic.index_id
JOIN sys.columns AS c
    ON ic.object_id = c.object_id
   AND ic.column_id = c.column_id
WHERE i.index_id > 0
  AND ic.is_included_column = 0
GROUP BY
    s.name,
    t.name,
    i.name,
    i.type_desc,
    i.is_unique,
    i.is_primary_key,
    i.is_unique_constraint
ORDER BY
    s.name,
    t.name,
    i.name;
GO


PRINT '=== 11. Aktualny kontekst wykonania ===';

SELECT
    DB_NAME() AS CurrentDatabase,
    SUSER_SNAME() AS ServerLogin,
    ORIGINAL_LOGIN() AS OriginalLogin,
    USER_NAME() AS DatabaseUser,
    SYSTEM_USER AS SystemUser,
    SESSION_USER AS SessionUser,
    CURRENT_USER AS CurrentUserName;
GO