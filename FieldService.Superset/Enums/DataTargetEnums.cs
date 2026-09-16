using FieldService.Queue.Types;
using FieldService.Superset.Entities;

namespace FieldService.Superset.Enums;

internal record SupersetDataTarget(
    string DatabaseName, 
    string SchemaName, 
    string ViewName
);

internal record ClientDatabaseTarget(
    SupersetDatabaseEngine DatabaseEngine,
    string EncryptedConnectionString
);

internal record ClientApiTarget(
    string EncryptedApiKey,
    string RecurringCronExpression,
    string Description,
    JobType JobType
);

internal enum SupersetDatabaseEngine
{
    PostgreSQL = 1,
    SqlServer = 2,
    MySQL = 3,
    MariaDB = 4,
    Oracle = 5,
    SQLite = 6,
    Snowflake = 10,
    BigQuery = 11,
    Redshift = 12,
    ClickHouse = 13,
    Databricks = 14,
    DuckDB = 15,
    Athena = 16

}