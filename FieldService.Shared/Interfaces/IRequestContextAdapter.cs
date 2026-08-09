using FieldService.Shared.Types;

namespace FieldService.Shared.Interfaces;

public interface IRequestContextAdapter<TContext>
{
    RequestContext Adapt(TContext context);
}