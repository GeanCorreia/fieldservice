namespace FieldService.Data.Interfaces;

public interface IMongoWriteDbContext : IMongoReadDbContext
{
    IUnitOfWork UnitOfWork { get; }
}
