using Hangfire.Common;
using Hangfire.States;

namespace FieldService.Queue.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableAutomaticRetryAttribute : JobFilterAttribute, IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is ScheduledState scheduledState &&
            scheduledState.Reason?.Contains("Retry", StringComparison.OrdinalIgnoreCase) == true)
        {
            var lastFailedState = context.TraversedStates.OfType<FailedState>().LastOrDefault();
            context.CandidateState = lastFailedState ??
                                     new FailedState(new InvalidOperationException("Automatic retries are disabled."));
        }
    }
}

