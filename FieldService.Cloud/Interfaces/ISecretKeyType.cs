using FieldService.Cloud.Entities;

namespace FieldService.Cloud.Interfaces;



public interface ISecretKeyType
{
    public TimeSpan? ExpiresIn => null;
    public SecretKeyType Type => SecretKeyType;
    
    public static SecretKeyType SecretKeyType { get; }
}

public interface ISecretKeyDefinition
{
    string Name { get; }
    Type SecretKeyType { get; }
    TimeSpan? ExpiresIn { get; }
}



