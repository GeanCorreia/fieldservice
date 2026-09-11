using System.Diagnostics;
using FieldService.Observability.Types;
using OpenTelemetry;

namespace FieldService.Observability.Services;

public class ExecutionContext
{
    public static Guid? RequestId
    {
        get => GetGuid("app.request_id");
        set => SetItem("app.request_id", value?.ToString("N"));
    }

    public static Guid? SessionId
    {
        get => GetGuid("app.session_id");
        set => SetItem("app.session_id", value?.ToString("N"));
    }

    public static Guid? TenantId
    {
        get => GetGuid("app.tenant_id");
        set => SetItem("app.tenant_id", value?.ToString("N"));
    }

    public static Guid? UserId
    {
        get => GetGuid("app.user_id");
        set => SetItem("app.user_id", value?.ToString("N"));
    }

    public static string? CorrelationId
    {
        get => GetString("correlation_id");
        set => SetItem("correlation_id", value);
    }
    

    public static RequestChannel Channel
    {
        get => GetEnum("app.request_channel", RequestChannel.Http);
        set => SetItem("app.request_channel", value.ToString());
    }

    public static string IpAddress
    {
        get => GetString("app.client.ip_address");
        set => SetItem("app.client.ip_address", value);
    }

    public static string UserAgent
    {
        get => GetString("app.client.user_agent");
        set => SetItem("app.client.user_agent", value);
    }

    public static DateTimeOffset Timestamp
    {
        get
        {
            var dtStr = GetString("app.request_timestamp");
            if (DateTimeOffset.TryParse(dtStr, out var dto))
                return dto;

            return DateTimeOffset.UtcNow;
        }
        set => SetItem("app.request_timestamp", value.ToString("o")); 
    }
    

    private static string GetString(string key)
    {
        var value = Activity.Current?.GetBaggageItem(key) 
                 ?? Activity.Current?.GetTagItem(key)?.ToString();

        return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }

    private static Guid? GetGuid(string key)
    {
        var val = GetString(key);
        return Guid.TryParse(val, out var guid) ? guid : null;
    }

    private static TEnum GetEnum<TEnum>(string key, TEnum defaultValue) where TEnum : struct, Enum
    {
        var val = GetString(key);
        return Enum.TryParse<TEnum>(val, ignoreCase: true, out var result) ? result : defaultValue;
    }

    private static void SetItem(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        
        Activity.Current?.SetTag(key, value);
        Baggage.Current.SetBaggage(key, value);
    }
}