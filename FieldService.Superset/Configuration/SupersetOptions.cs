namespace FieldService.Superset.Configuration;

public class SupersetOptions
{
    public const string SectionName = "Superset";
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = "Admin";
    public string Password { get; set; } = "admin";
    public int GuestTokenExpirationMinutes { get; set; } = 15;
    public int TimeoutSeconds { get; set; } = 10;
}