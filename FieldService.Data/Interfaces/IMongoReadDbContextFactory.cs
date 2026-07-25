namespace FieldService.Data.Interfaces;

public interface IMongoReadDbContextFactory
{
    IMongoReadDbContext Create(string moduleName);
}
