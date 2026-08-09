using FieldService.Shared.Message;
using Hangfire.Client;
using Hangfire.Common;
using FieldService.Shared.Types;
using Hangfire.Tags;

namespace FieldService.Queue.Filters;

public class MessageContextFilter : JobFilterAttribute, IClientFilter
{
    public void OnCreating(CreatingContext filterContext)
    {
        var messageArgument = GetMessageArgument(filterContext.Job.Args);

        if (messageArgument != null)
        {
            dynamic message = messageArgument;
            
            filterContext.SetJobParameter("MessageId", message.MessageId.ToString());
            filterContext.SetJobParameter("TenantId", message.TenantId.ToString());
            filterContext.SetJobParameter("TraceId", message.TraceId ?? string.Empty);
            filterContext.SetJobParameter("MessageType", message.MessageType);
            filterContext.SetJobParameter("SchemaVersion", message.SchemaVersion.ToString());
        }
    }

    public void OnCreated(CreatedContext filterContext)
    {
        if (filterContext.BackgroundJob is null)
            return;

        var messageArgument = GetMessageArgument(filterContext.Job.Args);
        if (messageArgument is null)
            return;

        dynamic message = messageArgument;
        filterContext.BackgroundJob.Id.AddTags(
            $"Tenant:{message.TenantId}",
            $"Type:{message.MessageType}",
            $"MessageId:{message.Id}");
    }

    private static object? GetMessageArgument(IReadOnlyList<object> args) =>
        args.FirstOrDefault(arg =>
            arg is not null &&
            arg.GetType().IsGenericType &&
            arg.GetType().GetGenericTypeDefinition() == typeof(Message<>));
}