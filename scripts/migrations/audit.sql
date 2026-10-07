CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE TABLE "AuditAccesses" (
        "Id" uuid NOT NULL,
        "RequestId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "Resource" text NOT NULL,
        "ResourceId" uuid,
        "Parameters" jsonb,
        CONSTRAINT "PK_AuditAccesses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE TABLE "AuditChanges" (
        "Id" uuid NOT NULL,
        "RequestId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "ResourceId" text NOT NULL,
        "Resource" text NOT NULL,
        "ChangedProperties" jsonb NOT NULL,
        CONSTRAINT "PK_AuditChanges" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE INDEX "idx_AuditAccesses_OccurredAt" ON "AuditAccesses" ("OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE INDEX "idx_AuditAccesses_RequestId" ON "AuditAccesses" ("RequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE INDEX "idx_AuditAccesses_UserId_TenantId" ON "AuditAccesses" ("UserId", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE INDEX "idx_AuditChanges_OccurredAt" ON "AuditChanges" ("OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE INDEX "idx_AuditChanges_RequestId" ON "AuditChanges" ("RequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    CREATE INDEX "idx_AuditChanges_UserId_TenantId" ON "AuditChanges" ("UserId", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805165053_InitialAudit') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260805165053_InitialAudit', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805180240_MakeAuditAccessTenantNullable') THEN
    ALTER TABLE "AuditAccesses" ALTER COLUMN "TenantId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260805180240_MakeAuditAccessTenantNullable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260805180240_MakeAuditAccessTenantNullable', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260829121441_UseTimestamptz') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260829121441_UseTimestamptz', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830012707_AddAuditDtoSchemaRegistry') THEN
    ALTER TABLE "AuditAccesses" RENAME COLUMN "Resource" TO "ResourceName";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830012707_AddAuditDtoSchemaRegistry') THEN
    ALTER TABLE "AuditAccesses" ADD "SchemaVersion" text NOT NULL DEFAULT '1.0.0';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830012707_AddAuditDtoSchemaRegistry') THEN
    CREATE TABLE "AuditDtoSchemas" (
        "ResourceName" text NOT NULL,
        "Version" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "Properties" jsonb NOT NULL,
        CONSTRAINT "PK_AuditDtoSchemas" PRIMARY KEY ("ResourceName", "Version")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830012707_AddAuditDtoSchemaRegistry') THEN
    CREATE INDEX "idx_AuditAccesses_ResourceName_SchemaVersion" ON "AuditAccesses" ("ResourceName", "SchemaVersion");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830012707_AddAuditDtoSchemaRegistry') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260830012707_AddAuditDtoSchemaRegistry', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925022409_AuditRequest') THEN
    CREATE TABLE "AuditRequest" (
        "Id" uuid NOT NULL,
        "JwtId" character varying(256) NOT NULL,
        "IpAddressHash" character varying(256) NOT NULL,
        "UserAgentHash" character varying(256) NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "Channel" integer NOT NULL,
        "Resource" character varying(512) NOT NULL,
        "Successful" boolean NOT NULL,
        "StatusCode" integer NOT NULL,
        CONSTRAINT "PK_AuditRequest" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925022409_AuditRequest') THEN
    CREATE INDEX "idx_SessionActivity_Timestamp" ON "AuditRequest" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925022409_AuditRequest') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260925022409_AuditRequest', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925025917_RefatoracaoAuditRequest') THEN
    ALTER TABLE "AuditRequest" ADD "SessionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925025917_RefatoracaoAuditRequest') THEN
    ALTER TABLE "AuditRequest" ADD "UserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925025917_RefatoracaoAuditRequest') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260925025917_RefatoracaoAuditRequest', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925043629_AddAuditAccessSchemaForeignKey') THEN
    LOCK TABLE "AuditAccesses" IN ACCESS EXCLUSIVE MODE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925043629_AddAuditAccessSchemaForeignKey') THEN
    LOCK TABLE "AuditDtoSchemas" IN ACCESS EXCLUSIVE MODE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925043629_AddAuditAccessSchemaForeignKey') THEN
    INSERT INTO "AuditDtoSchemas" ("ResourceName", "Version", "CreatedAt", "Properties")
    SELECT DISTINCT a."ResourceName", a."SchemaVersion", NOW(), '{}'::jsonb
    FROM "AuditAccesses" a
    LEFT JOIN "AuditDtoSchemas" s
        ON s."ResourceName" = a."ResourceName"
       AND s."Version" = a."SchemaVersion"
    WHERE a."ResourceName" IS NOT NULL
      AND a."SchemaVersion" IS NOT NULL
      AND s."ResourceName" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925043629_AddAuditAccessSchemaForeignKey') THEN
    ALTER TABLE "AuditAccesses" ADD CONSTRAINT "fk_AuditAccesses_AuditDtoSchemas_ResourceName_SchemaVersion" FOREIGN KEY ("ResourceName", "SchemaVersion") REFERENCES "AuditDtoSchemas" ("ResourceName", "Version") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925043629_AddAuditAccessSchemaForeignKey') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260925043629_AddAuditAccessSchemaForeignKey', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927044740_MudacaoResourceIdString') THEN
    ALTER TABLE "AuditAccesses" ALTER COLUMN "ResourceId" TYPE text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927044740_MudacaoResourceIdString') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260927044740_MudacaoResourceIdString', '9.0.1');
    END IF;
END $EF$;
COMMIT;

