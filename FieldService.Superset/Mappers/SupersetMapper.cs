using FieldService.Shared.Types;
using FieldService.Superset.Dtos;
using FieldService.Superset.Dtos.SupersetApiRequestDto;
using FieldService.Superset.Dtos.SupersetApiResponseDto;
using FieldService.Superset.Utils;

namespace FieldService.Superset.Mappers;

internal static class SupersetMapper
{
    public static SupersetLoginApiRequest Map(this UserTenantDto user)
    {
        return new SupersetLoginApiRequest(
            Username: SupersetUsernameResolver.ResolveUsername(user),
            Password: SupersetUsernameResolver.ResolvePassword(user)
        );
    }

    public static SupersetDashboardDto Map(this SupersetDashboardApiResponse item)
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

    public static SupersetChartDto Map(this SupersetChartApiResponse item)
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

    public static SupersetDatasetDto Map(this SupersetDatasetApiResponse item)
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

    public static SupersetSavedQueryDto Map(this SupersetSavedQueryApiResponse item)
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