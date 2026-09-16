using System;
using System.Threading.Tasks;
using FieldService.Authentication.Entities;
using FieldService.Authentication.Interfaces;
using FieldService.Authentication.Types;
using FieldService.Shared.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using User = Microsoft.Graph.Models.User;

namespace FieldService.Authentication.Services;

internal sealed class AzureEntraIdentityProvider : IIdentityProvider
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _b2cDomain;

   
    public AzureEntraIdentityProvider(
        GraphServiceClient graphClient,
        IConfiguration configuration)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        _b2cDomain = configuration.GetSection("AzureAdB2C")["Domain"] 
                     ?? throw new InvalidOperationException(
                         "Configuration not found: AzureAdB2C.Domain.");
    }

    public async Task<string> CreateUserAsync(
        Guid userId, 
        Email email,
        string name)
    {
        var newUser = new User
        {
            AccountEnabled = true,
            DisplayName = name.Trim(),
            Mail = email.Value,
            
            UserPrincipalName = $"{userId}@{_b2cDomain}", 
            
            Identities = new List<ObjectIdentity>
            {
                new()
                {
                    SignInType = "emailAddress",
                    IssuerAssignedId = email.Value 
                }
            }
        };
        
        var createdUser = await _graphClient.Users.PostAsync(newUser);

        if (createdUser == null)
        {
            throw new Exception("Unable to create user");
        }

        return createdUser.Id;
    }

    public async Task DisableUserAsync(string externalUserId)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);

        var updateUser = new User
        {
            AccountEnabled = false 
        };

        await _graphClient.Users[externalUserId].PatchAsync(updateUser);
    }

    public async Task EnableUserAsync(string externalUserId)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);

        var updateUser = new User
        {
            AccountEnabled = true 
        };

        await _graphClient.Users[externalUserId].PatchAsync(updateUser);
    }

    public async Task DeleteUserAsync(string externalUserId)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalUserId);
        
        await _graphClient.Users[externalUserId].DeleteAsync();
    }
}