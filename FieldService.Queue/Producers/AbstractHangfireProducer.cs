using Hangfire;

namespace FieldService.Queue.Producers;

public abstract class AbstractHangfireProducer
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    protected AbstractHangfireProducer(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
    }

    protected IBackgroundJobClient BackgroundJobClient => _backgroundJobClient;
}


