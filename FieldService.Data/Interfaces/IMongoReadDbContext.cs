using MongoDB.Driver;

namespace FieldService.Data.Interfaces;

public interface IMongoReadDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName);
}
