using FieldService.Data.Interfaces;
using MongoDB.Driver;

namespace FieldService.Data.Contexts;

internal sealed class MongoWriteDbContext(IMongoDatabase database, IUnitOfWork unitOfWork)
    : IMongoWriteDbContext
{
    public IMongoDatabase Database { get; } = database;
    public IUnitOfWork UnitOfWork { get; } = unitOfWork;

    public IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        return Database.GetCollection<TDocument>(collectionName);
    }
}
