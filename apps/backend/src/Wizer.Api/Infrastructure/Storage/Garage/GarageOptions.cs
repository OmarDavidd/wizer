namespace Wizer.Api.Infrastructure.Storage.Garage;

public sealed class GarageOptions
{
    public const string SectionName = "Garage";

    /// <summary>Internal S3 endpoint, for example <c>http://garage:3900</c>.</summary>
    public string S3Endpoint { get; set; } = string.Empty;

    /// <summary>Admin API, for example <c>http://garage:3903</c>. Only its unauthenticated <c>/health</c> is used.</summary>
    public string AdminEndpoint { get; set; } = string.Empty;
}
