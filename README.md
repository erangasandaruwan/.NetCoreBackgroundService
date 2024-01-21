# Implement background tasks with .Net Core IHostedService & BackgroundService 

<p>Background tasks and scheduled jobs are something that need to use in any of application including microservices. The difference when using a such within microservices is that it can be implemented the background task in a separate process or thread else in another container for hosting where it decouples the functionalities.</p>
<p>As background tasks can run concurrently with foreground operations, developers typically assign them to run do so to improve performance. There are common examples such as to periodically clean up unused data after a specified duration, run an periodical query on a database or implement an long running subscriber.</p>
<p>Within any of the application context, those tasks can be identified as Hosted Services, as they are services or logics that is possible to host within another standalone host, application or microservice. Further, the hosted service simply means a class with the background task logic.</p>
</p>One of the easiest ways to start implementing background tasks into your ASP.NET application is through the IHostedService interface. This interface allows to run background tasks at specific intervals continuously, which you can designate with every application instance or set up as a standalone project. However, IHostedService is typically relegated to short-running tasks.</p>
</p>Since .NET Core 2.0, the framework provides a new interface named IHostedService helping us to easily implement hosted services. The basic idea is that we can define, register multiple background tasks as hosted services where those can be run in the background while your web host or host is running.</p>
</p>In contrast, BackgroundService was introduced as an extension for long running or concurrent tasks.</p>

#### **There are essentially two abstractions to know about**
- **Ihostedservice** - This is the base interface for running services.
- **BackgroundService** - This is a base and an abstract class which implements IHostedService but includes some extra functionalities are designed for long-running background tasks.

#### How it implemnted with .Net

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IHostedService"/>.
    /// </summary>
    public abstract class BackgroundService : IHostedService, IDisposable
    {
        private Task? _executeTask;
        private CancellationTokenSource? _stoppingCts;

        /// <summary>
        /// Gets the Task that executes the background operation.
        /// </summary>
        /// <remarks>
        /// Will return <see langword="null"/> if the background operation hasn't started.
        /// </remarks>
        public virtual Task? ExecuteTask => _executeTask;

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        /// <remarks>See <see href="https://docs.microsoft.com/dotnet/core/extensions/workers">Worker Services in .NET</see> for implementation guidelines.</remarks>
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous Start operation.</returns>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            // Create linked token to allow cancelling executing task from provided token
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            _executeTask = ExecuteAsync(_stoppingCts.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            if (_executeTask.IsCompleted)
            {
                return _executeTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous Stop operation.</returns>
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executeTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts!.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                var tcs = new TaskCompletionSource<object>();
                using CancellationTokenRegistration registration = cancellationToken.Register(s => ((TaskCompletionSource<object>)s!).SetCanceled(), tcs);
                // Do not await the _executeTask because cancelling it will throw an OperationCanceledException which we are explicitly ignoring
                await Task.WhenAny(_executeTask, tcs.Task).ConfigureAwait(false);
            }

        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            _stoppingCts?.Cancel();
        }
    }
}
```

#### Consider between Host & WebHost
| Host | WebHost |
| --- | --- |
| .NET Core 2.1 and later versions support IHost for background processes with plain console apps. | After ASP.Net MVC, ASP.NET Core 1.x and 2.x support IWebHost for background processes in web apps. | 
| A Host was introduced in .NET Core 2.1. Basically, a Host allows you to have a similar infrastructure than what you have with WebHost (dependency injection, hosted services, etc.), but in this case, you just want to have a simple and lighter process as the host, with nothing related to MVC, Web API or HTTP server features. | A WebHost in ASP.NET Core 2.0 is the infrastructure artifact you use to provide HTTP server features to your process, such as when you're implementing an MVC web app or Web API service. It provides all the new infrastructure goodness in ASP.NET Core, enabling you to use dependency injection, insert middlewares in the request pipeline, and similar. |

#### Configure application as a WebHost
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        .ConfigureLogging(l => l.ClearProviders())
        .UseDefaultServiceProvider(opt => opt.ValidateScopes = false); 
}
```
### How to configure and use **IhostedService** and **BackgroundService**

First we implement IHostedService
1. SampleHostedService Implement the IHostedService interface.
2. It Initialize a variable called _timer to use in the StartAsync and StopAsync methods from the implemented IHostedInterface. The timer runs the ActionToBePerformed method every five seconds.
3. The ActionToBePerformed method block contains the code to print “HostedService - Simple service resumed after 5 seconds.”

```csharp
﻿using Microsoft.Extensions.Hosting;

namespace HostedService.Lib.HostedService
{
    public class SampleHostedService : IHostedService
    {
        private Timer _timer = null;
        private readonly IHostApplicationLifetime _applicationLifetime;
        
        public SampleHostedService(IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public Task StartAsync(CancellationToken cancellingToken)
        {
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
            Console.WriteLine(state.ToString() + " - Simple service resumed after 5 seconds.");
        }
    }
}
```
Now we extend BackgroundService
1. SampleBackgroundService extend the abstract class BackgroundService.
2. It runs the method ExecuteAsync at the beginig and invoke the method code until it's end. Here it will run in a never ending while loop.
3. The ExecuteAsync method will sleep a 5 seconds and print “BackgroundService - Simple service resumed after 5 seconds.”
   
```csharp
﻿using NLog;
using Microsoft.Extensions.Hosting;

namespace HostedService.Lib.BackgroundServices
{
    public class SampleBackgroundService : BackgroundService
    {
        public SampleBackgroundService(
            IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                Thread.Sleep(5000);
                Console.WriteLine("BackgroundService - Simple service resumed after 5 seconds.");
            }
        }
    }
}
```
