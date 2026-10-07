CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901223007_InitialBroker') THEN
    CREATE TABLE "BrokerOutbox" (
        "MessageId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "DispatchedAt" timestamp with time zone,
        "ExpiresAt" timestamp with time zone,
        "Message" jsonb NOT NULL,
        CONSTRAINT "PK_BrokerOutbox" PRIMARY KEY ("MessageId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901223007_InitialBroker') THEN
    CREATE INDEX "idx_BrokerOutbox_CreatedAt" ON "BrokerOutbox" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901223007_InitialBroker') THEN
    CREATE INDEX "idx_BrokerOutbox_DispatchedAt" ON "BrokerOutbox" ("DispatchedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901223007_InitialBroker') THEN
    CREATE INDEX "idx_BrokerOutbox_ExpiresAt" ON "BrokerOutbox" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260901223007_InitialBroker') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260901223007_InitialBroker', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907025638_RefatorarBrokerOutbox') THEN
        ALTER TABLE "BrokerOutbox"
        ADD COLUMN "PublishContext" jsonb NOT NULL DEFAULT '{}'::jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907025638_RefatorarBrokerOutbox') THEN
        ALTER TABLE "BrokerOutbox"
        ALTER COLUMN "PublishContext" DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907025638_RefatorarBrokerOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260907025638_RefatorarBrokerOutbox', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908010526_AlteracoBrokerOutbox') THEN
    ALTER TABLE "BrokerOutbox" DROP COLUMN "PublishContext";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908010526_AlteracoBrokerOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260908010526_AlteracoBrokerOutbox', '9.0.1');
    END IF;
END $EF$;
COMMIT;

