using Dapper;
using FieldService.Superset.Broker;
using FieldService.Superset.Configuration;
using FieldService.Superset.Dtos;
using FieldService.Superset.Entities;
using FieldService.Superset.Interfaces;
using FieldService.Superset.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FieldService.Superset.Cqrs.Commands.CreateSupersetTenant;

internal class CreateSupersetTenantHandler : IRequestHandler<CreateSupersetTenantCommand>
{
    private readonly IMediator _mediator;
    private readonly string _connectionString;
    private readonly ISupersetTenantInstanceProcessingLock _supersetInstanceLock;
    private readonly ILogger<CreateSupersetTenantHandler> _logger;
    private readonly ISupersetAuthService _supersetAuthService;
    private readonly ISupersetService _supersetService;
    private readonly ISupersetSecretService _supersetSecretService;
    private readonly ISupersetTenantDeploymentService _supersetTenantDeploymentService;
    private readonly IDatabaseService _databaseService;
    private readonly SupersetOptions _supersetOptions;
    private readonly CreateSupersetTenantContainerJobConsumer.CreateSupersetTenantContainerJobProducer _createSupersetTenantContainerJobProducer;
    private readonly SupersetTenantDeploymentBrokerProducer _supersetTenantDeploymentBrokerProducer;
    
    
    public async Task Handle(
        CreateSupersetTenantCommand request, 
        CancellationToken cancellationToken)
    {
        var tenantConfig = await _supersetService.GetSupersetTenantByTenantIdAsync(request.CreateParams.TenantId, cancellationToken);
        if(tenantConfig != null)
        {
            return;
        }
        
        var tenantId = request.CreateParams.TenantId;
        var lockAcquired = await _supersetInstanceLock.AcquireLock(tenantId, cancellationToken);
        if (!lockAcquired)
        {
            throw new InvalidOperationException($"Could not acquire lock for tenant {tenantId}. " +
                                                $"Another instance creation process might be running.");
        }
        
        
        var connectionString = await _supersetSecretService.CreateDatabaseConnectionString(tenantId, request.UserId, cancellationToken);
        
        await CreateDatabaseAsync(
            connectionString.databaseParams, 
            connectionString.ConnectionString,
            cancellationToken);


        if (request.CreateParams.InstanceTier != InstanceTier.OnDemand)
        {
            await CreateInstanceAsync(
                request.CreateParams,
                _supersetOptions.SecretKeyId,
                connectionString.ConnectionStringId,
                request.UserId,
                cancellationToken);
            
            return;
        }
        
        await HandleDefaultSuperset(
            request, 
            connectionString.ConnectionStringId, cancellationToken);
       
    }

    private async Task HandleDefaultSuperset(
        CreateSupersetTenantCommand request,
        Guid connectionStringId,
        CancellationToken cancellationToken
        )
    {
        
        var supersetRole = await _supersetAuthService.GetTenantScopeRole(
            request.CreateParams.TenantId,
            cancellationToken);

        if (supersetRole == null)
        {
            await _supersetAuthService.CreateTenantScopeRole(
                request.CreateParams.TenantId,
                cancellationToken);
        }

        var tenantConfig = SupersetTenantConfig.Create(
            request.CreateParams.TenantId,
            request.CreateParams.InstanceTier,
            connectionStringId,
            _supersetOptions.ResourceId,
            _supersetOptions.BaseUrl,
            _supersetOptions.SecretKeyId
        );
        
        await _supersetService.SaveAsync(
            tenantConfig,
            cancellationToken);
        
        await _supersetInstanceLock.ReleaseLock(request.CreateParams.TenantId, cancellationToken);
        
        var message = new SupersetTenantDeploymentPayload(
            request.CreateParams.TenantId,
            _supersetOptions.BaseUrl,
            _supersetOptions.ResourceId);

        try
        {
            await _supersetTenantDeploymentBrokerProducer.PublishAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
           //no exception should be thrown here, as the deployment notice is not critical for the tenant creation
        }
        
        
    }
    
    
    
    private async Task CreateInstanceAsync(
        SupersetTenantCreateParams supersetTenantCreateParams,
        Guid supersetSecretKeyId,
        Guid connectionStringId,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
       var payload = new CreateSupersetTenantContainerJobPayload(
           supersetTenantCreateParams, 
           connectionStringId, 
           userId);
       
       var job = new CreateSupersetTenantContainerJob(payload);
       
       await _supersetInstanceLock.ReleaseLock(supersetTenantCreateParams.TenantId, cancellationToken);
       
        _createSupersetTenantContainerJobProducer.Publish(job);
    }
    
    private async Task CreateDatabaseAsync(
        SupersetDatabaseParams supersetDatabaseParams,
        string tenantDbConnectionString,
        CancellationToken cancellationToken = default)
    {
        var databaseName = supersetDatabaseParams.DatabaseName;
        var user = supersetDatabaseParams.Username;
        var password = supersetDatabaseParams.Password;
        
        var sanitizedDbName = databaseName.Replace("\"", "\"\"");
        var hasDatabase = await _databaseService.DatabaseExistsAsync(sanitizedDbName, cancellationToken);
        if (!hasDatabase)
        {
            var createDbSql = $"CREATE DATABASE \"{sanitizedDbName}\";";
            await _databaseService.ExecuteCommandAsync(createDbSql, cancellationToken: cancellationToken);
        }
        
        
        string Lit(string val) => $"'{val.Replace("'", "''")}'"; 
        string Id(string val) => val.Replace("\"", "\"\"");     
        
        var setupTenantDbSql = $@"
            -- Criação dos Schemas
            CREATE SCHEMA IF NOT EXISTS ""{Id(SupersetTenantConfig.DataSchemaPrefix)}"";
            CREATE SCHEMA IF NOT EXISTS ""{Id(SupersetTenantConfig.MetadataSchemaPrefix)}"";
            CREATE SCHEMA IF NOT EXISTS ""{Id(SupersetTenantConfig.MockedDataSchemaPrefix)}"";

            -- Gestão de Usuário e Senha com segurança via PL/pgSQL
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = {Lit(user)}) THEN
                    EXECUTE format('CREATE USER %I WITH PASSWORD %L', {Lit(user)}, {Lit(password)});
                ELSE
                    EXECUTE format('ALTER USER %I WITH PASSWORD %L', {Lit(user)}, {Lit(password)});
                END IF;
            END
            $$;

            -- Permissão de Conexão no Banco
            EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', CURRENT_DATABASE(), {Lit(user)});

            -----------------------------------------------------------------------------
            -- 1. SCHEMA METADATA (Escrita / Ownership Total para o Superset)
            -----------------------------------------------------------------------------
            EXECUTE format('ALTER SCHEMA %I OWNER TO %I', {Lit(SupersetTenantConfig.MetadataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('GRANT ALL ON SCHEMA %I TO %I', {Lit(SupersetTenantConfig.MetadataSchemaPrefix)}, {Lit(user)});

            -----------------------------------------------------------------------------
            -- 2. SCHEMAS DATA e MOCKED DATA (Apenas Leitura / Read-Only para o Superset)
            -----------------------------------------------------------------------------
            -- Permissão de navegação no Schema
            EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});

            -- Permissão SELECT em tabelas existentes (se houver)
            EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});

            -- Permissão SELECT em tabelas FUTURAS criadas pelo seu módulo de Injeção de Dados (Admin)
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON TABLES TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON TABLES TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});

            -- Permissão SELECT em SEQUENCES FUTURAS (Auto-Increment)
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON SEQUENCES TO %I', {Lit(SupersetTenantConfig.DataSchemaPrefix)}, {Lit(user)});
            EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE CURRENT_USER IN SCHEMA %I GRANT SELECT ON SEQUENCES TO %I', {Lit(SupersetTenantConfig.MockedDataSchemaPrefix)}, {Lit(user)});
        ";

        await _databaseService.ExecuteCommandAsync(
            setupTenantDbSql, 
            sanitizedDbName, 
            cancellationToken: cancellationToken);
    }
    
    
}