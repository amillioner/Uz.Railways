namespace Rail.Data.Models;

public class JwtSettings
{
    public const string SectionName = "JWT";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "RailApi";
    public string Audience { get; set; } = "RailApi";
    public int ExpirationHours { get; set; } = 24;
}