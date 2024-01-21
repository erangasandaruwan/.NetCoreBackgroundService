using Microsoft.Extensions.Hosting;


namespace HostedService.Lib.HostedService
{
    public class SampleHostedService : IHostedService
    {
        private Timer _timer = null;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private CancellationToken _cancellationToken;
        
        public SampleHostedService(IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public Task StartAsync(CancellationToken cancellingToken)
        {
            _cancellationToken = cancellingToken;
            _timer = new Timer(ActionToBePerformed, "HostedService", TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellingToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        async void ActionToBePerformed(object state)
        {
            if (!await WaitForAppStartup(_applicationLifetime, _cancellationToken))
            {
                return;
            }

            Console.WriteLine(state.ToString() + " - Simple service resumed after 5 seconds.");
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
