# RecurrentTasks

This lightweight library allows you to run simple background tasks with specified intervals in your ASP.NET application. 

Each task is a separate `Task`, which sleeps in background for a while, wakes up, perform some job and sleeps again.

Ideal, when you don't need to run many/heavy tasks and don't want to use "big" solutions with persistence and other bells and whistles.

Written for **NET Core** (support for ASP.NET 5 and ASP.NET Core 1.0 and 2.0 is dropped since v6, use [v5.0.0 release](https://github.com/justdmitry/RecurrentTasks/releases/tag/v5.0.0) if you need support for old frameworks).

[![Build status](https://ci.appveyor.com/api/projects/status/uucaowlbcxybi4v6/branch/master?svg=true)](https://ci.appveyor.com/project/justdmitry/recurrenttasks/branch/master) 
[![NuGet](https://img.shields.io/nuget/v/RecurrentTasks.svg?maxAge=86400&style=flat)](https://www.nuget.org/packages/RecurrentTasks/) 
[![codecov](https://codecov.io/gh/justdmitry/RecurrentTasks/branch/master/graph/badge.svg)](https://codecov.io/gh/justdmitry/RecurrentTasks)

## Main features

* TargetFrameworks: `netstandard2.0` and `netcoreapp3.1`
* Start and Stop your task at any time;
* [`IHostedService`](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/background-tasks-with-ihostedservice) implemented for NET Core 2.0 (and above) app lifetime support
* CancelationToken may be used for Stopping;
* First run (after Start) is delayed at random value (10-30 sec, customizable) to prevent app freeze during statup;
* Run "immediately" (without waiting for next scheduled time);
* Change run interval while running;
* Single-execution-at-a-time: A task already running will wait before running again (timer for "next" run will start only after "current" run completes);
* `RunStatus` property contains:
    * last/next run times;
    * last run result (success / exception);
    * last success run time;
    * last exception;
    * total failed runs counter.

## Usage

### 1. Create new task class

```csharp
public class MyFirstTask : IRunnable
{
    private ILogger logger;

    public MyFirstTask(ILogger<MyFirstTask> logger)
    {
        this.logger = logger;
    }
    
    public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
    {
        // Place your code here
    }
}
```

You can add any parameters to constructor, while they are resolvable from DI container (including scope-lifetime services, because new scope is created for every task run).

By default, new instance of `IRunnable` is created for every task run, but you may change lifetime in `AddTask` (see below). Use `IServiceProvider` passed to `RunAsync` to obtain scope-wide services if you force your task be singleton.

### 2. Register and start your task in `Startup.cs`


```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddTask<MyFirstTask>(o => o.AutoStart(TimeSpan.FromMinutes(5)));
    ...
}

```

And voila! Your task will run every 5 minutes. Until your application ends, of course.

`AddTask` adds your `MyFirstTask` to DI container with transient lifetime (new instance will be created for every task run). Pass desired lifetime to `AddTask()` to override: 
```csharp
services.AddTask<MyFirstTask>(
  o => o.AutoStart(TimeSpan.FromMinutes(5)),
  ServiceLifetime.Singleton)`.
```

### Run immediately

Anywhere in you app:

```csharp
// obtain reference to your task
var myTask = serviceProvider.GetService<ITask<MyFirstTask>>();

// poke it
if (myTask.IsStarted)
{
    myTask.TryRunImmediately();
}
```

## Installation

Use NuGet package [RecurrentTasks](https://www.nuget.org/packages/RecurrentTasks/)

### Dependencies

* Microsoft.Extensions.DependencyInjection.Abstractions, v2.0.0 (for `netstandard2.0`) / v3.1.0 (for `netcoreapp3.1`)
* Microsoft.Extensions.Hosting.Abstractions, v2.0.0 / v3.1.0
* Microsoft.Extensions.Logging.Abstractions, v2.0.0 / v3.1.0

## Testing

Tests can be run with `dotnet test`.
