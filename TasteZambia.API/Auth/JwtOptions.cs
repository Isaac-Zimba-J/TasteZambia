namespace TasteZambia.API.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    /// <summary>At least 32 bytes. Never a literal in code; production supplies Jwt__SigningKey.</summary>
    public required string SigningKey { get; init; }

    public int AccessMinutes { get; init; } = 60;
    public int RefreshDays { get; init; } = 30;
}
