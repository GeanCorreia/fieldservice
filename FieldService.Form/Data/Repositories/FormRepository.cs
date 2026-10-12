using System.Reflection;
using FieldService.Data.Interfaces;
using FieldService.Form.Events;
using FieldService.Form.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Form.Data.Repositories;

internal sealed class FormRepository(
    FormDbContext dbContext,
    ISqlUnitOfWork<FormDbContext> unitOfWork) : IFormRepository
{
    private static readonly FieldInfo EventsField = typeof(Entities.Form)
        .GetField("_events", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not find '_events' backing field in Form entity.");

    public async Task<Entities.Form?> GetFormAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id, nameof(id));

        return await dbContext.Forms
                   .AsNoTracking()
                   .Include("_events")
                   .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
               ;
    }

    public async Task<List<Entities.Form>> GetFormsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(tenantId, nameof(tenantId));

        return await dbContext.Forms
            .AsNoTracking()
            .Include("_events")
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveFormAsync(
        Entities.Form form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        var existing = await dbContext.Forms
            .Include("_events")
            .FirstOrDefaultAsync(x => x.Id == form.Id, cancellationToken);

        if (existing is null)
        {
            await dbContext.Forms.AddAsync(form, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(form);
            MergeNewEvents(existing, form);
        }

        await unitOfWork.PersistChangesAsync(cancellationToken);
    }

    private static void MergeNewEvents(Entities.Form target, Entities.Form source)
    {
        var targetEvents = GetEvents(target);
        var sourceEvents = GetEvents(source);

        if (sourceEvents.Count == 0)
            return;

        var existingIds = targetEvents.Select(e => e.Id).ToHashSet();
        foreach (var formEvent in sourceEvents)
        {
            if (existingIds.Add(formEvent.Id))
            {
                targetEvents.Add(formEvent);
            }
        }
    }

    private static List<FormEvent> GetEvents(Entities.Form form)
    {
        var value = EventsField.GetValue(form);
        if (value is List<FormEvent> events)
            return events;

        throw new InvalidOperationException("Form entity has invalid '_events' backing field type.");
    }

    private static void ValidateId(Guid id, string paramName)
    {
        if (id == default)
            throw new ArgumentException("Id cannot be empty.", paramName);
    }
}
