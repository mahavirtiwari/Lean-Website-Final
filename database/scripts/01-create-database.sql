/* =============================================================================
   MSME Competitive (LEAN) Scheme portal - database provisioning
   -----------------------------------------------------------------------------
   Run once, as a sysadmin, on the target SQL Server instance before the first
   deployment. The schema itself is created by EF Core migrations on first start
   of the API, so this script only prepares the database and the login.

   Adjust the file paths and sizes to your instance before running.
   ============================================================================= */

SET NOCOUNT ON;
GO

/* --------------------------------------------------------------- database -- */

IF DB_ID(N'LeanPortal') IS NULL
BEGIN
    PRINT 'Creating database LeanPortal…';

    /*
      Sizes are a starting point for a content portal: the content itself is
      small, but the audit trail and uploads metadata grow steadily.
    */
    CREATE DATABASE [LeanPortal]
    ON PRIMARY
    (
        NAME     = N'LeanPortal_Data',
        FILENAME = N'D:\SQLData\LeanPortal_Data.mdf',
        SIZE     = 512MB,
        FILEGROWTH = 128MB
    )
    LOG ON
    (
        NAME     = N'LeanPortal_Log',
        FILENAME = N'D:\SQLLogs\LeanPortal_Log.ldf',
        SIZE     = 128MB,
        FILEGROWTH = 64MB
    );
END
ELSE
    PRINT 'Database LeanPortal already exists - skipping creation.';
GO

/* ------------------------------------------------------------- settings -- */

ALTER DATABASE [LeanPortal] SET RECOVERY FULL;

/* Read Committed Snapshot keeps the public site reading while editors write. */
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'LeanPortal' AND is_read_committed_snapshot_on = 0)
BEGIN
    PRINT 'Enabling READ_COMMITTED_SNAPSHOT…';
    ALTER DATABASE [LeanPortal] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    ALTER DATABASE [LeanPortal] SET READ_COMMITTED_SNAPSHOT ON;
    ALTER DATABASE [LeanPortal] SET MULTI_USER;
END
GO

ALTER DATABASE [LeanPortal] SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE [LeanPortal] SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE [LeanPortal] SET AUTO_CLOSE OFF;
ALTER DATABASE [LeanPortal] SET AUTO_SHRINK OFF;
GO

/* ------------------------------------------------------------------ login -- */

/*
  Preferred: the IIS application pool identity, so no password is stored anywhere.
  Replace DOMAIN\SERVER$ or the app pool identity to match your environment:

    IIS AppPool\LeanPortal-Api   for a local application pool identity
    DOMAIN\svc-leanportal        for a domain service account
*/
DECLARE @AppLogin sysname = N'IIS APPPOOL\LeanPortal-Api';

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @AppLogin)
BEGIN
    PRINT 'Creating login ' + @AppLogin + '…';
    DECLARE @sql nvarchar(max) =
        N'CREATE LOGIN ' + QUOTENAME(@AppLogin) + N' FROM WINDOWS WITH DEFAULT_DATABASE = [LeanPortal];';
    EXEC sp_executesql @sql;
END
ELSE
    PRINT 'Login already exists - skipping.';
GO

/* -------------------------------------------------------------------- user -- */

USE [LeanPortal];
GO

DECLARE @AppLogin sysname = N'IIS APPPOOL\LeanPortal-Api';
DECLARE @sql nvarchar(max);

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @AppLogin)
BEGIN
    PRINT 'Creating database user…';
    SET @sql = N'CREATE USER ' + QUOTENAME(@AppLogin) + N' FOR LOGIN ' + QUOTENAME(@AppLogin) + N';';
    EXEC sp_executesql @sql;
END

/*
  The application needs to read and write data, and to create the schema when
  EF Core applies migrations on first start.

  If you prefer to apply migrations manually (Database:MigrateAndSeedOnStartup
  set to false, and the generated script run by a DBA), drop db_ddladmin below
  and grant only db_datareader and db_datawriter.
*/
SET @sql = N'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@AppLogin) + N';';
EXEC sp_executesql @sql;

SET @sql = N'ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@AppLogin) + N';';
EXEC sp_executesql @sql;

SET @sql = N'ALTER ROLE db_ddladmin ADD MEMBER ' + QUOTENAME(@AppLogin) + N';';
EXEC sp_executesql @sql;

PRINT 'Permissions granted.';
GO

/* --------------------------------------------------------------- verify -- */

SELECT
    DB_NAME()                                        AS [Database],
    (SELECT recovery_model_desc FROM sys.databases WHERE name = DB_NAME())          AS RecoveryModel,
    (SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME()) AS ReadCommittedSnapshot,
    (SELECT COUNT(*) FROM sys.tables)                AS TableCount;

PRINT '';
PRINT 'Provisioning complete.';
PRINT 'The schema is created by EF Core migrations the first time the API starts,';
PRINT 'or by running database\scripts\02-schema.sql if you apply migrations manually.';
GO
