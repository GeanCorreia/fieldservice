using FieldService.Data.Interfaces;
using FieldService.Data.Services;
using MongoDB.Driver;

namespace FieldService.Data.Contexts;

internal sealed class MongoReadDbContextFactory(
    IMongoClient mongoClient,
    IMongoDatabaseNameResolver databaseNameResolver) : IMongoReadDbContextFactory
{
    public IMongoReadDbContext Create(string moduleName)
    {
        var databaseName = databaseNameResolver.Resolve(moduleName);
        var database = mongoClient.GetDatabase(databaseName);
        return new MongoReadDbContext(database);
    }
}
