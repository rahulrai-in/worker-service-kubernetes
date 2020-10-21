using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OfficeCountdownClock
{
    public sealed class TcpHealthProbeService : BackgroundService
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly TcpListener _listener;
        private readonly ILogger<TcpHealthProbeService> _logger;

        public TcpHealthProbeService(
            HealthCheckService healthCheckService,
            ILogger<TcpHealthProbeService> logger,
            IConfiguration config)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger;

            // Attach TCP listener to the port in configuration
            var port = config.GetValue<int?>("HealthProbe:TcpPort") ?? 5000;
            _listener = new TcpListener(IPAddress.Any, port);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Started health check service.");
            await Task.Yield();
            _listener.Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                // Respond to the pending TCP calls every second.
                await UpdateHeartbeatAsync(stoppingToken);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private async Task UpdateHeartbeatAsync(CancellationToken token)
        {
            try
            {
                // Get health check results
                var result = await _healthCheckService.CheckHealthAsync(token);
                var isHealthy = result.Status == HealthStatus.Healthy;
                _logger.LogInformation($"Health check status: {isHealthy}");

                if (isHealthy && !_listener.Server.IsBound)
                {
                    _logger.LogWarning("Application healthy - starting TCP listener.");
                    _listener.Start();
                }

                if (!isHealthy && _listener.Server.IsBound)
                {
                    _logger.LogWarning("Application unhealthy - stopping TCP listener.");
                    _listener.Stop();
                }

                while (_listener.Server.IsBound && _listener.Pending())
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    var stream = client.GetStream();
                    var writer = new StreamWriter(stream);
                    await writer.WriteLineAsync("OK");
                    await stream.FlushAsync(token);
                    client.Close();
                    _logger.LogInformation("Processed health check request.");
                }

                _logger.LogDebug("Heartbeat check executed.");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An error occurred while checking heartbeat.");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}