namespace FieldService.Superset.Configuration;

public class SupersetOptions
{
    public const string SectionName = "Superset";
    public ContainerAppResourceRequirements ContainerAppResourceRequirements { get; set; } = new ContainerAppResourceRequirements();
    public DataBaseHost DataBaseHost { get; set; } = new DataBaseHost();
    public SupersetProvisioningOptions Provisioning { get; set; } = new SupersetProvisioningOptions();
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = "Admin";
    public string Password { get; set; } = "admin";
    public int GuestTokenExpirationMinutes { get; set; } = 1440;
    public int TimeoutSeconds { get; set; } = 30;
    public string DomainType { get; set; } = "Path";
    public string RouteId { get; set; } = string.Empty;
    public string ClusterId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string RoutePath { get; set; } = string.Empty;
    public string PathRemovePrefix { get; set; } = string.Empty;
    
    public int LockTtlMinutes { get; set; } = 5;
}

public class ContainerAppResourceRequirements
{
    public const string SectionName = "Superset:ContainerAppResources";
    public string SupersetDockerImage { get; set; } = "apache/superset:latest";
    public double Cpu { get; set; } = 1.0;
    public string Memory { get; set; } = "2.0Gi";
}

public class DataBaseHost
{
    public const string SectionName = "Superset:DataBaseHost";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5432;
}

public class SupersetProvisioningOptions
{
    public int ContainerPort { get; set; } = 8088;
    public int StartupHealthCheckMaxAttempts { get; set; } = 30;
    public int StartupHealthCheckDelaySeconds { get; set; } = 10;
    public int GunicornWorkers { get; set; } = 1;
    public int GunicornTimeoutSeconds { get; set; } = 120;
    public bool ScaleDownAfterProvisioning { get; set; } = true;
    public string AdminEmail { get; set; } = "admin@fieldservice.local";
    public string AdminFirstName { get; set; } = "Superset";
    public string AdminLastName { get; set; } = "Admin";
}
