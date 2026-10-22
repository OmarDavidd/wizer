using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Wizer.Api.Infrastructure.Antivirus.ClamAV;

/// <summary>Sends clamd's <c>zPING</c> command over TCP and expects <c>PONG</c>.</summary>
public sealed class ClamAvHealthCheck(IOptions<ClamAvOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            return HealthCheckResult.Unhealthy("ClamAV host is not configured.");
        }

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(settings.Host, settings.Port, cancellationToken);
            await using var stream = client.GetStream();
            await stream.WriteAsync("zPING\0"u8.ToArray(), cancellationToken);

            var buffer = new byte[16];
            var read = await stream.ReadAsync(buffer, cancellationToken);
            var reply = Encoding.ASCII.GetString(buffer, 0, read).TrimEnd('\0', '\n');

            return reply == "PONG"
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("ClamAV returned an unexpected reply.");
        }
        catch (SocketException exception)
        {
            return HealthCheckResult.Unhealthy("ClamAV is not reachable.", exception);
        }
    }
}
