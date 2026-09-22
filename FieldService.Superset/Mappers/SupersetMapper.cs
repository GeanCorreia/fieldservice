using FieldService.Superset.Dtos;

namespace FieldService.Superset.Mappers;

internal static class SupersetMapper
{
    public static SupersetDashboardDto Map(this SupersetDashboardApiItem item)
    {
        return new SupersetDashboardDto(
            Resource: new SupersetResourceDto(
                ResourceId: item.Id.ToString(),
                Name: item.DashboardTitle,
                ResourceType: "dashboard",
                Description: item.Slug
            ),
            IsPublished: item.Published,
            EmbeddedUuid: item.Embedded?.FirstOrDefault()?.Uuid
        );
    }

    public static SupersetChartDto Map(this SupersetChartApiItem item)
    {
        return new SupersetChartDto(
            Resource: new SupersetResourceDto(
                ResourceId: item.Id.ToString(),
                Name: item.SliceName,
                ResourceType: "chart"
            ),
            VizType: item.VizType
        );
    }

    public static SupersetDatasetDto Map(this SupersetDatasetApiItem item)
    {
        return new SupersetDatasetDto(
            Resource: new SupersetResourceDto(
                ResourceId: item.Id.ToString(),
                Name: item.TableName,
                ResourceType: "dataset"
            ),
            TableName: item.TableName,
            Schema: item.Schema
        );
    }

    public static SupersetSavedQueryDto Map(this SupersetSavedQueryApiItem item)
    {
        return new SupersetSavedQueryDto(
            Resource: new SupersetResourceDto(
                ResourceId: item.Id.ToString(),
                Name: item.Label,
                ResourceType: "saved_query"
            ),
            Sql: item.Sql
        );
    }
}