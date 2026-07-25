using FieldService.Data.Interfaces;
using FieldService.Data.Services;
using MongoDB.Driver;

namespace FieldService.Data.Contexts;

internal sealed class MongoWriteDbContextFactory(
    IMongoClient mongoClient,
    IMongoDatabaseNameResolver databaseNameResolver,
    IUnitOfWork unitOfWork) : IMongoWriteDbContextFactory
{
    public IMongoWriteDbContext Create(string moduleName)
    {
        var databaseName = databaseNameResolver.Resolve(moduleName);
        var database = mongoClient.GetDatabase(databaseName);
        return new MongoWriteDbContext(database, unitOfWork);
    }
}
