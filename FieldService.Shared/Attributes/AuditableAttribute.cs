namespace FiledService.Shared.Attributes;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AuditableAttribute : Attribute
{
    public string Resource { get; }
    public string ResourceIdPropertyName { get; }
    
    public AuditableAttribute(string resource, string resourceIdPropertyName)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource is required.", nameof(resource));

        if (string.IsNullOrWhiteSpace(resourceIdPropertyName))
            throw new ArgumentException("ResourceIdPropertyName is required.", nameof(resourceIdPropertyName));

        Resource = resource;
        ResourceIdPropertyName = resourceIdPropertyName;
    }
}