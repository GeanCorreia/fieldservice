namespace FieldService.Data.Interfaces;

public interface IMongoWriteDbContextFactory
{
    IMongoWriteDbContext Create(string moduleName);
}
