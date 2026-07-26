using Microsoft.EntityFrameworkCore;

namespace FieldService.Data.Interfaces;

public interface ISqlUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
}
