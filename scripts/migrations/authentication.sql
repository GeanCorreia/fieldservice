CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE TABLE "Session" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ExternalId" text NOT NULL,
        "Provider" integer NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "RevocationReason" integer,
        "LastActivityAt" timestamp with time zone,
        CONSTRAINT "PK_Session" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE TABLE "UserAuthentication" (
        "UserId" uuid NOT NULL,
        "ExternalId" character varying(256) NOT NULL,
        "Provider" integer NOT NULL,
        CONSTRAINT "PK_UserAuthentication" PRIMARY KEY ("UserId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE TABLE "SessionActivity" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "JwtId" character varying(256) NOT NULL,
        "IpAddressHash" character varying(256) NOT NULL,
        "UserAgentHash" character varying(256),
        "Timestamp" timestamp with time zone NOT NULL,
        "Channel" integer NOT NULL,
        "CorrelationId" uuid NOT NULL,
        CONSTRAINT "PK_SessionActivity" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SessionActivity_Session_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Session" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE TABLE "UserTenantEvent" (
        "Id" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "TenantName" character varying(256) NOT NULL,
        "Status" integer NOT NULL,
        "EndedAt" timestamp with time zone,
        CONSTRAINT "PK_UserTenantEvent" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserTenantEvent_UserAuthentication_UserId" FOREIGN KEY ("UserId") REFERENCES "UserAuthentication" ("UserId") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE INDEX "idx_Session_ExpiresAt" ON "Session" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE INDEX "idx_Session_UserId_TenantId" ON "Session" ("UserId", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE INDEX "idx_SessionActivity_SessionId" ON "SessionActivity" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE INDEX "idx_SessionActivity_Timestamp" ON "SessionActivity" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE UNIQUE INDEX "idx_UserAuthentication_ExternalId_Provider" ON "UserAuthentication" ("ExternalId", "Provider");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE INDEX "idx_UserTenantEvent_UserId" ON "UserTenantEvent" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    CREATE INDEX "idx_UserTenantEvent_UserId_TenantId_CreatedAt" ON "UserTenantEvent" ("UserId", "TenantId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165055_InitialAuthentication') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260805165055_InitialAuthentication', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806152925_RenameSessionActivityCorrelationIdToRequestId') THEN
    ALTER TABLE "SessionActivity" RENAME COLUMN "CorrelationId" TO "RequestId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806152925_RenameSessionActivityCorrelationIdToRequestId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260806152925_RenameSessionActivityCorrelationIdToRequestId', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829121453_UseTimestamptz') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260829121453_UseTimestamptz', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830014706_SyncAuthenticationModel') THEN
    ALTER TABLE "SessionActivity" DROP CONSTRAINT "FK_SessionActivity_Session_SessionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830014706_SyncAuthenticationModel') THEN
    ALTER TABLE "Session" DROP COLUMN "LastActivityAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830014706_SyncAuthenticationModel') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260830014706_SyncAuthenticationModel', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260924223546_RefatoracaoSessionActivity') THEN
    UPDATE "SessionActivity" SET "UserAgentHash" = '' WHERE "UserAgentHash" IS NULL;
    ALTER TABLE "SessionActivity" ALTER COLUMN "UserAgentHash" SET NOT NULL;
    ALTER TABLE "SessionActivity" ALTER COLUMN "UserAgentHash" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260924223546_RefatoracaoSessionActivity') THEN
    ALTER TABLE "SessionActivity" ADD "Resource" character varying(512) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260924223546_RefatoracaoSessionActivity') THEN
    ALTER TABLE "SessionActivity" ADD "StatusCode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260924223546_RefatoracaoSessionActivity') THEN
    ALTER TABLE "SessionActivity" ADD "Successful" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260924223546_RefatoracaoSessionActivity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260924223546_RefatoracaoSessionActivity', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925021418_DropSessionActivityTable') THEN
    DROP TABLE IF EXISTS "SessionActivity";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925021418_DropSessionActivityTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260925021418_DropSessionActivityTable', '9.0.1');
    END IF;
END $EF$;
COMMIT;

