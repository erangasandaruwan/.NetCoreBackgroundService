using NLog;
using Microsoft.Extensions.Hosting;

namespace HostedService.Lib.BackgroundServices
{
    public class SampleBackgroundService : BackgroundService
    {
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IHostApplicationLifetime _applicationLifetime;

        public SampleBackgroundService(
            IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!await WaitForAppStartup(_applicationLifetime, stoppingToken))
            {
                return;
            }

            while (true)
            {
                Thread.Sleep(5000);
                Console.WriteLine("BackgroundService - Simple service resumed after 5 seconds.");
            }
        }

        static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
        {
            var startedSource = new TaskCompletionSource();
            var cancelledSource = new TaskCompletionSource();

            using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());
            using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

            Task completedTask = await Task.WhenAny(
                startedSource.Task,
                cancelledSource.Task).ConfigureAwait(false);

            // If the completed tasks was the "app started" task, return true, otherwise false
            return completedTask == startedSource.Task;
        }
    }
}
