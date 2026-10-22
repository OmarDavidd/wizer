namespace Wizer.Api.Infrastructure.Storage.MinIO;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    /// <summary>Internal S3 endpoint, for example <c>http://minio:9000</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;
}
