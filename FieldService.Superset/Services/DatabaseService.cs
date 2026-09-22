using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using FieldService.Superset.Interfaces;


namespace FieldService.Superset.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    
    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SupersetHostDatabase")
            ?? throw new NullReferenceException("Connection string 'SupersetHostDatabase' not found in configuration.");
    }
    
    private IDbConnection CreateAndOpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void ExecuteCommand(string sqlCommand, object? parameters = null)
    {
        using var connection = CreateAndOpenConnection();
        connection.Execute(sqlCommand, parameters);
    }

    public async Task ExecuteCommandAsync(string sqlCommand, object? parameters = null)
    {
        using var connection = CreateAndOpenConnection();
        await connection.ExecuteAsync(sqlCommand, parameters);
    }
}