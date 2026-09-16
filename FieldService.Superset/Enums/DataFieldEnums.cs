using FieldService.Superset.Entities;

namespace FieldService.Superset.Enums;

internal record SupersetResourceField(
    string Name,
    SupersetFieldType Type,
    bool IsRequired = false
);

internal enum SupersetFieldType
{
    String = 1,
    Uuid = 2,
    Integer = 10,
    BigInteger = 11,
    Decimal = 12,
    Float = 13,
    Date = 20,
    Time = 21,
    DateTime = 22,
    DateTimeWithTimeZone = 23,
    TimeSpan = 24, 
    Boolean = 30,
    Json = 40,
    Array = 41,
    GeoPoint = 50,     // Latitude / Longitude
    GeoPolygon = 51      // Valores monetários com precisão
}