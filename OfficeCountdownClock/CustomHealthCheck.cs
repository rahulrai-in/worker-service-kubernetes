using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OfficeCountdownClock
{
    public class CustomHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            // Randomly sets to true or false
            var flipFlop = new Random().Next(2) == 1;
            return Task.FromResult(flipFlop
                ? new HealthCheckResult(HealthStatus.Healthy)
                : new HealthCheckResult(HealthStatus.Unhealthy, "Random error",
                    data: new Dictionary<string, object> {{nameof(flipFlop), false}}));
        }
    }
}