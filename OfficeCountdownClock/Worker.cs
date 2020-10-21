using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace OfficeCountdownClock
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fiveOClock = new TimeSpan(17, 0, 0);
            var nineOClock = new TimeSpan(9, 0, 0);
            var timeZone = DateTimeZoneProviders.Tzdb["Australia/Sydney"];
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = Instant.FromDateTimeUtc(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));
                var nowLocal = now.InZone(timeZone).ToDateTimeUnspecified();
                if (nowLocal.DayOfWeek == DayOfWeek.Saturday || nowLocal.DayOfWeek == DayOfWeek.Sunday)
                {
                    _logger.LogInformation("{Time}: Cheers to the weekend!", nowLocal);
                }
                else
                {
                    var message = nowLocal switch
                    {
                        _ when nowLocal.Hour >= 9 && nowLocal.Hour <= 16 =>
                        $"Hang in there, just {fiveOClock.Subtract(nowLocal.TimeOfDay).TotalMinutes} minutes to go!",

                        _ when nowLocal.Hour < 9 =>
                        $"Get ready, office hours start in {nineOClock.Subtract(nowLocal.TimeOfDay).TotalMinutes} minutes!",

                        _ => "You are done for the day, relax!"
                    };

                    _logger.LogInformation("{Time}: " + message, nowLocal);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}