namespace FieldService.Http.Configuration;

public class HttpOptions
{
    public const string SectionName = "Http";
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

}