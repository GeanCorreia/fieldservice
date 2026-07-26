namespace FieldService.Data.Interfaces;

public interface IMongoWriteDbContext : IMongoReadDbContext
{
    IMongoUnitOfWork UnitOfWork { get; }
}
