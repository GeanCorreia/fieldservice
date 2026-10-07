CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE TABLE "StoredFileCategories" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Code" text NOT NULL,
        "MaxSizeInBytes" bigint,
        "AllowedContentTypes" jsonb NOT NULL,
        "Version" text NOT NULL,
        "MinimumRequiredRole" integer,
        "AllowedPermissions" jsonb NOT NULL,
        CONSTRAINT "PK_StoredFileCategories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE TABLE "StoredFiles" (
        "Id" uuid NOT NULL,
        "FileCategoryId" uuid NOT NULL,
        "UploadedByUserId" uuid NOT NULL,
        "StatusChangedByUserId" uuid,
        "UploadedAt" timestamp with time zone NOT NULL,
        "StatusUpdatedAt" timestamp with time zone,
        "HashMd5" text NOT NULL,
        "FileName" text NOT NULL,
        "ContentType" text NOT NULL,
        "Size" bigint NOT NULL,
        "Provider" integer NOT NULL,
        "Status" integer NOT NULL,
        "StoragePath" text NOT NULL,
        CONSTRAINT "PK_StoredFiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StoredFiles_StoredFileCategories_FileCategoryId" FOREIGN KEY ("FileCategoryId") REFERENCES "StoredFileCategories" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE INDEX "IX_StoredFileCategories_TenantId" ON "StoredFileCategories" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE UNIQUE INDEX "IX_StoredFileCategories_TenantId_Code" ON "StoredFileCategories" ("TenantId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE INDEX "IX_StoredFiles_FileCategoryId" ON "StoredFiles" ("FileCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE INDEX "IX_StoredFiles_Status" ON "StoredFiles" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE UNIQUE INDEX "IX_StoredFiles_StoragePath" ON "StoredFiles" ("StoragePath");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    CREATE INDEX "IX_StoredFiles_UploadedByUserId" ON "StoredFiles" ("UploadedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260913044314_StorageRetryServices') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260913044314_StorageRetryServices', '9.0.1');
    END IF;
END $EF$;
COMMIT;

