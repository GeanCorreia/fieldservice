using FieldService.Data;
using FieldService.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FieldService.InfraTest.Data;

public sealed class MongoConnectionTests
{
    [Fact]
    public async Task Should_connect_to_mongodb_and_resolve_read_write_contexts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDb"] = "mongodb://admin:admin123@localhost:27017/fieldservice?authSource=admin",
                ["MongoDb:Database"] = "fieldservice",
                ["MongoDb:ModuleDatabases:Orders"] = "fieldservice_orders"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMongoModule(configuration);

        using var provider = services.BuildServiceProvider();

        var database = provider.GetRequiredService<IMongoDatabase>();
        var readFactory = provider.GetRequiredService<IMongoReadDbContextFactory>();
        var writeFactory = provider.GetRequiredService<IMongoWriteDbContextFactory>();

        var pingResult = await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        var readContext = readFactory.Create("Orders");
        var writeContext = writeFactory.Create("Orders");

        Assert.Equal(1, pingResult["ok"].ToInt32());
        Assert.Equal("fieldservice_orders", readContext.Database.DatabaseNamespace.DatabaseName);
        Assert.Equal("fieldservice_orders", writeContext.Database.DatabaseNamespace.DatabaseName);
        Assert.False(writeContext.UnitOfWork.HasActiveTransaction);
    }
}
