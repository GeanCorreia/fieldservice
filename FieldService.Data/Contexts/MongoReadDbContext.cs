using FieldService.Data.Interfaces;
using MongoDB.Driver;

namespace FieldService.Data.Contexts;

internal sealed class MongoReadDbContext(IMongoDatabase database) : IMongoReadDbContext
{
    public IMongoDatabase Database { get; } = database;

    public IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        return Database.GetCollection<TDocument>(collectionName);
    }
}
