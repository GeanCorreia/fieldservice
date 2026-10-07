CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE TABLE "PermissionEvents" (
        "Id" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "PermissionModule" text NOT NULL,
        "PermissionResource" text NOT NULL,
        "PermissionAction" text NOT NULL,
        "Type" integer NOT NULL,
        CONSTRAINT "PK_PermissionEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE TABLE "RoleEvents" (
        "Id" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Role" text NOT NULL,
        CONSTRAINT "PK_RoleEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE TABLE "UserSuspensionEvents" (
        "Id" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Source" integer NOT NULL,
        "StartedAt" timestamp with time zone,
        "EndedAt" timestamp with time zone,
        "OriginalSuspensionEventId" uuid,
        CONSTRAINT "PK_UserSuspensionEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_PermissionEvents_CreatedAt" ON "PermissionEvents" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_PermissionEvents_Type" ON "PermissionEvents" ("Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_PermissionEvents_UserId_TenantId" ON "PermissionEvents" ("UserId", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_RoleEvents_CreatedAt" ON "RoleEvents" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_RoleEvents_UserId_TenantId" ON "RoleEvents" ("UserId", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_UserSuspensionEvents_CreatedAt" ON "UserSuspensionEvents" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_UserSuspensionEvents_OriginalSuspensionEventId" ON "UserSuspensionEvents" ("OriginalSuspensionEventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_UserSuspensionEvents_Type" ON "UserSuspensionEvents" ("Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    CREATE INDEX "idx_UserSuspensionEvents_UserId_TenantId" ON "UserSuspensionEvents" ("UserId", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165135_InitialAuthorization') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260805165135_InitialAuthorization', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829121503_UseTimestamptz') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260829121503_UseTimestamptz', '9.0.1');
    END IF;
END $EF$;
COMMIT;

