namespace FieldService.Shared.Types;

/// <summary>
/// Defines a resource that can be protected with permissions.
/// Used by modules to register their resources.
/// </summary>
public interface IResourceDefinition
{
    /// <summary>
    /// The name of the resource (e.g., "users", "projects").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets all valid actions for this resource.
    /// </summary>
    IEnumerable<string> GetActions();
}
