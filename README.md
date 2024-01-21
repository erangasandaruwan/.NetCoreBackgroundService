# Implement background tasks with .Net Core IHostedService & BackgroundService 

<p>Background tasks and scheduled jobs are something that need to use in any of application including microservices. The difference when using a such within microservices is that it can be implemented the background task in a separate process or thread else in another container for hosting where it decouples the functionalities.</p>
<p>As background tasks can run concurrently with foreground operations, developers typically assign them to run do so to improve performance. There are common examples such as to periodically clean up unused data after a specified duration, run an periodical query on a database or implement an long running subscriber.</p>
<p>Within any of the application context, those tasks can be identified as Hosted Services, as they are services or logics that is possible to host within another standalone host, application or microservice. Further, the hosted service simply means a class with the background task logic.</p>
</p>One of the easiest ways to start implementing background tasks into your ASP.NET application is through the IHostedService interface. This interface allows to run background tasks at specific intervals continuously, which you can designate with every application instance or set up as a standalone project. However, IHostedService is typically relegated to short-running tasks.</p>
</p>Since .NET Core 2.0, the framework provides a new interface named IHostedService helping us to easily implement hosted services. The basic idea is that we can define, register multiple background tasks as hosted services where those can be run in the background while your web host or host is running.</p>
</p>In contrast, BackgroundService was introduced as an extension for long running or concurrent tasks.</p>

#### **There are essentially two abstractions to know about**
- **Ihostedservice** - a "bare bones" interface for running services.
- **BackgroundService** - a base class which implements IHostedService but includes some extra bells and whistles and is designed for long-running background tasks.

### Consider between Host & WebHost
| Host | WebHost |
| --- | --- |
| .NET Core 2.1 and later versions support IHost for background processes with plain console apps. | After ASP.Net MVC, ASP.NET Core 1.x and 2.x support IWebHost for background processes in web apps. | 
| A Host was introduced in .NET Core 2.1. Basically, a Host allows you to have a similar infrastructure than what you have with WebHost (dependency injection, hosted services, etc.), but in this case, you just want to have a simple and lighter process as the host, with nothing related to MVC, Web API or HTTP server features. | A WebHost in ASP.NET Core 2.0 is the infrastructure artifact you use to provide HTTP server features to your process, such as when you're implementing an MVC web app or Web API service. It provides all the new infrastructure goodness in ASP.NET Core, enabling you to use dependency injection, insert middlewares in the request pipeline, and similar. |

#### With WebHost
```
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
