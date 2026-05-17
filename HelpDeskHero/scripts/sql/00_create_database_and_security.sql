USE master;
GO

IF DB_ID(N'HelpDeskHeroDb') IS NULL
BEGIN
    CREATE DATABASE HelpDeskHeroDb;
END
GO

IF SUSER_ID(N'hdh_migrator') IS NULL
BEGIN
    CREATE LOGIN hdh_migrator
    WITH PASSWORD = 'Change_Me_Strong_Migrator_Password_123!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

IF SUSER_ID(N'hdh_app') IS NULL
BEGIN
    CREATE LOGIN hdh_app
    WITH PASSWORD = 'Change_Me_Strong_App_Password_123!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

USE HelpDeskHeroDb;
GO

IF USER_ID(N'hdh_migrator') IS NULL
BEGIN
    CREATE USER hdh_migrator FOR LOGIN hdh_migrator;
END
GO

IF USER_ID(N'hdh_app') IS NULL
BEGIN
    CREATE USER hdh_app FOR LOGIN hdh_app;
END
GO

IF IS_ROLEMEMBER(N'db_owner', N'hdh_migrator') = 0
BEGIN
    ALTER ROLE db_owner ADD MEMBER hdh_migrator;
END
GO

IF DATABASE_PRINCIPAL_ID(N'role_helpdeskhero_app') IS NULL
BEGIN
    CREATE ROLE role_helpdeskhero_app;
END
GO

IF IS_ROLEMEMBER(N'role_helpdeskhero_app', N'hdh_app') = 0
BEGIN
    ALTER ROLE role_helpdeskhero_app ADD MEMBER hdh_app;
END
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO role_helpdeskhero_app;
GO
