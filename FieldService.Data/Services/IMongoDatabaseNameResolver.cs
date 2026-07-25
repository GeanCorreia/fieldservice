namespace FieldService.Data.Services;

internal interface IMongoDatabaseNameResolver
{
    string Resolve(string moduleName);
}
