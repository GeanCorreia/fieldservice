CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922041021_FirstMigration') THEN
    CREATE TABLE "SecretKeys" (
        "Id" uuid NOT NULL,
        "Type" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SecretKeys" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922041021_FirstMigration') THEN
    CREATE TABLE "SecretKeyEvents" (
        "Id" uuid NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "SecretKeyId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "EventType" integer NOT NULL,
        "Name" character varying(128),
        "TypeName" character varying(256),
        CONSTRAINT "PK_SecretKeyEvents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SecretKeyEvents_SecretKeys_SecretKeyId" FOREIGN KEY ("SecretKeyId") REFERENCES "SecretKeys" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922041021_FirstMigration') THEN
    CREATE INDEX "IX_SecretKeyEvents_SecretKeyId" ON "SecretKeyEvents" ("SecretKeyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922041021_FirstMigration') THEN
    CREATE INDEX "IX_SecretKeyEvents_SecretKeyId_OccurredAt" ON "SecretKeyEvents" ("SecretKeyId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922041021_FirstMigration') THEN
    CREATE INDEX "IX_SecretKeys_TenantId" ON "SecretKeys" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922041021_FirstMigration') THEN
    CREATE INDEX "IX_SecretKeys_TenantId_Type" ON "SecretKeys" ("TenantId", "Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260922041021_FirstMigration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260922041021_FirstMigration', '9.0.1');
    END IF;
END $EF$;
COMMIT;

