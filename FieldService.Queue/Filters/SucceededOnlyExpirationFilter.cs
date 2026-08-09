using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace FieldService.Queue.Filters;

public sealed class SucceededOnlyExpirationFilter(TimeSpan retention) : JobFilterAttribute, IApplyStateFilter
{
    private readonly TimeSpan _retention = retention;

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is SucceededState)
            context.JobExpirationTimeout = _retention;
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }
}
