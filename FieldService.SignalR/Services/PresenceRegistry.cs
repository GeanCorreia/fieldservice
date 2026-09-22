using FieldService.Authentication.Interfaces;
using FieldService.Cache.Interfaces;
using FieldService.SignalR.Configuration;
using FieldService.SignalR.Interfaces;
using FieldService.SignalR.Types;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace FieldService.SignalR.Services;

internal class PresenceRegistry : IPresenceRegistry
    {


        private readonly HybridCache hybridCache;
        private readonly HybridCacheEntryOptions _cacheOptions;
    
        public  PresenceRegistry(
            IOptions<SignalROptions> configuration,
            HybridCache hybridCache)
        {
            
            this.hybridCache = hybridCache;
           
            _cacheOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(configuration.Value.TtlPresenceRegistryInHours),
                LocalCacheExpiration = TimeSpan.FromMinutes(configuration.Value.TtlLocalExpirationInMinutes)
            }; 
            
        }
    
        private const string ConnectionPrefix = "signalr:conn:";
        private const string SessionConnectionsPrefix = "signalr:session:";
        private const string UserConnectionsPrefix = "signalr:user:";
        private const string TenantConnectionsPrefix = "signalr:tenant:";
        
    
      
        private static string ConnectionKey(string connectionId) => $"{ConnectionPrefix}{connectionId}";
        private static string SessionConnectionsKey(Guid sessionId) => $"{SessionConnectionsPrefix}{sessionId}";
        private static string UserConnectionsKey(Guid userId) => $"{UserConnectionsPrefix}{userId}";
        private static string TenantConnectionsKey(Guid tenantId) => $"{TenantConnectionsPrefix}{tenantId}";
    
        public async Task RegisterAsync(SignalRConnectionContext connectionContext, CancellationToken ct = default)
        {
            var connKey = ConnectionKey(connectionContext.ConnectionId);
            var sessionKey = SessionConnectionsKey(connectionContext.SessionId);
            var userKey = UserConnectionsKey(connectionContext.UserId);
            var tenantKey = TenantConnectionsKey(connectionContext.TenantId);
            
            await hybridCache.SetAsync(connKey, connectionContext, _cacheOptions, cancellationToken: ct);
            await hybridCache.SetAsync(sessionKey, connectionContext, _cacheOptions, cancellationToken: ct);
            
            var existingContext = await GetConnectionContextBySessionIdAsync(
                connectionContext.SessionId, 
                ct);
            
            if (existingContext is not null && existingContext.ConnectionId != connectionContext.ConnectionId)
            {
                await UnregisterConnectionAsync(existingContext.ConnectionId, ct);
            }
            
            var userConnections = await hybridCache.GetOrCreateAsync(
                userKey,
                _ => ValueTask.FromResult(new HashSet<string>()),
                _cacheOptions,
                cancellationToken: ct);
            
            var updatedUserConnections = new HashSet<string>(userConnections) { connectionContext.ConnectionId };
            await hybridCache.SetAsync(userKey, updatedUserConnections, _cacheOptions, cancellationToken: ct);
            
            var tenantConnections = await hybridCache.GetOrCreateAsync(
                tenantKey,
                _ => ValueTask.FromResult(new HashSet<string>()),
                _cacheOptions,
                cancellationToken: ct);
    
            var updatedTenantConnections = new HashSet<string>(tenantConnections) { connectionContext.ConnectionId };
            await hybridCache.SetAsync(tenantKey, updatedTenantConnections, _cacheOptions, cancellationToken: ct);
        }
    
        public async Task UnregisterConnectionAsync(string connectionId, CancellationToken ct = default)
        {
            var context = await GetConnectionContextByConnectionIdAsync(connectionId, ct);
            if (context is null) return;
    
            var connKey = ConnectionKey(connectionId);
            var sessionKey = SessionConnectionsKey(context.SessionId);
            var userKey = UserConnectionsKey(context.UserId);
            var tenantKey = TenantConnectionsKey(context.TenantId);
            
            await hybridCache.RemoveAsync(connKey, ct);
            await hybridCache.RemoveAsync(sessionKey, ct);
            
            var userConnections = await hybridCache.GetOrCreateAsync(
                userKey,
                _ => ValueTask.FromResult(new HashSet<string>()),
                _cacheOptions,
                cancellationToken: ct);
    
            var updatedUserConnections = new HashSet<string>(userConnections);
            updatedUserConnections.Remove(connectionId);
    
            if (updatedUserConnections.Count > 0)
                await hybridCache.SetAsync(userKey, updatedUserConnections, _cacheOptions, cancellationToken: ct);
            else
                await hybridCache.RemoveAsync(userKey, ct);
            
            var tenantConnections = await hybridCache.GetOrCreateAsync(
                tenantKey,
                _ => ValueTask.FromResult(new HashSet<string>()),
                _cacheOptions,
                cancellationToken: ct);
    
            var updatedTenantConnections = new HashSet<string>(tenantConnections);
            updatedTenantConnections.Remove(connectionId);
    
            if (updatedTenantConnections.Count > 0)
                await hybridCache.SetAsync(tenantKey, updatedTenantConnections, _cacheOptions, cancellationToken: ct);
            else
                await hybridCache.RemoveAsync(tenantKey, ct);
        }
    
        public async Task UnregisterUserAsync(Guid userId, CancellationToken ct = default)
        {
            var userKey = UserConnectionsKey(userId);
            var userConnections = await hybridCache.GetOrCreateAsync(
                userKey,
                _ => ValueTask.FromResult(new HashSet<string>()),
                _cacheOptions,
                cancellationToken: ct);
    
            foreach (var connId in userConnections.ToList())
            {
                await UnregisterConnectionAsync(connId, ct);
            }
    
            await hybridCache.RemoveAsync(userKey, ct);
        }
    
        public async Task<SignalRConnectionContext?> GetConnectionContextBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
        {
            var key = SessionConnectionsKey(sessionId);
    
            return await hybridCache.GetOrCreateAsync<SignalRConnectionContext?>(
                key,
                _ => ValueTask.FromResult<SignalRConnectionContext?>(null),
                _cacheOptions,
                cancellationToken: ct);
        }
    
        public async Task<SignalRConnectionContext?> GetConnectionContextByConnectionIdAsync(string connectionId, CancellationToken ct = default)
        {
            var key = ConnectionKey(connectionId);
    
            return await hybridCache.GetOrCreateAsync<SignalRConnectionContext?>(
                key,
                _ => ValueTask.FromResult<SignalRConnectionContext?>(null),
                _cacheOptions,
                cancellationToken: ct);
        }
    
        public async Task<IReadOnlyCollection<SignalRConnectionContext>> GetConnectionContextsByTenantAsync(Guid tenantId, CancellationToken ct = default)
        {
            var tenantKey = TenantConnectionsKey(tenantId);
            var tenantConnections = await hybridCache.GetOrCreateAsync(
                tenantKey,
                _ => ValueTask.FromResult(new HashSet<string>()),
                _cacheOptions,
                cancellationToken: ct);
    
            return await GetContextsFromConnectionIdsAsync(tenantConnections, ct);
        }
    
        public async Task<IReadOnlyCollection<SignalRConnectionContext>> GetConnectionContextsByUserAsync(Guid userId, CancellationToken ct = default)
        {
            var userKey = UserConnectionsKey(userId);
            var userConnections = await hybridCache.GetOrCreateAsync(
                userKey,
                _ => ValueTask.FromResult(new HashSet<string>()),
                _cacheOptions,
                cancellationToken: ct);
    
            return await GetContextsFromConnectionIdsAsync(userConnections, ct);
        }
    
        private async Task<IReadOnlyCollection<SignalRConnectionContext>> GetContextsFromConnectionIdsAsync(
            IEnumerable<string> connectionIds, 
            CancellationToken ct)
        {
            var tasks = connectionIds.Select(id => GetConnectionContextByConnectionIdAsync(id, ct));
            var results = await Task.WhenAll(tasks);
    
            return results
                .Where(ctx => ctx is not null)
                .Select(ctx => ctx!)
                .ToList();
        }
    }   
   