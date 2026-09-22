namespace FieldService.Shared.Types;

public sealed record Permission
{
    public string Module { get; }
    public string Resource { get; }
    public string Action { get; }

    public Permission(string module, string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Module is required.", nameof(module));

        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource is required.", nameof(resource));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        Module = module.ToLowerInvariant();
        Resource = resource.ToLowerInvariant();
        Action = action.ToLowerInvariant();
    }

    public static Permission Create(string module, string resource, string action)
        => new(module, resource, action);
    
    public static Permission Create(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            throw new ArgumentException("Policy name is required.", nameof(policyName));

        var parts = policyName.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            throw new ArgumentException("Invalid policy name format.", nameof(policyName));

        return new Permission(parts[0], parts[1], parts[2]);
    }

    public override string ToString() => $"{Module}:{Resource}:{Action}";
    
    public string PolicyName => ToString();
}
