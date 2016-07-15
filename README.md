# RecurrentTasks

This lightweight library allows you to run simple background tasks with specified intervals in your ASP.NET application. 

Each task is a separate `Task`, which sleeps in background for a while, wakes up, perform some job and sleeps again.

Ideal, when you don't need to run many/heavy tasks and don't want to use "big" solutions with persistence and other bells and whistles.

Written for **ASP.NET Core** (ASP.NET 5, ASP.NET vNext).

[![Build status](https://ci.appveyor.com/api/projects/status/uucaowlbcxybi4v6/branch/master?svg=true)](https://ci.appveyor.com/project/justdmitry/recurrenttasks/branch/master) [![NuGet](https://img.shields.io/nuget/v/RecurrentTasks.svg?maxAge=2592000&style=flat)](https://www.nuget.org/packages/RecurrentTasks/)

## Main features

* Start and Stop your task at any time;
* First run (after Start) is delayed at random value (10-30 sec, customizable) to prevent app freeze during statup;
* Run "immediately" (without waiting for next scheduled time);
* Change run interval while running;
* `RunStatus` property (extendable) contains:
    * last/next run times;
    * last run result (success / exception);
    * last success run time;
    * last exception;
    * total failed runs counter.

## Usage

### 1. Create new task class

```csharp
public class MyFirstTask : TaskBase<TaskRunStatus>
{
    public MyFirstTask(ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
        : base(loggerFactory, TimeSpan.FromMinutes(5), serviceScopeFactory)
    {
        // Nothing
    }
    
    protected override void Run(IServiceProvider serviceProvider, TaskRunStatus runStatus)
    {
        // Place your code here
    }
}
```

### 2. Register and start your task in `Startup.cs`


```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddSingleton<MyFirstTask>();
    ...
}
    
public void Configure(IApplicationBuilder app, ...)
{
    ...
    app.ApplicationServices.GetRequiredService<MyFirstTask>().Start();
    ...
}
```

And viola! Your task will run every 5 minutes (second param when calling :base constructor). Until you application alive, of course.


## Installation

Use NuGet package [RecurrentTasks](https://www.nuget.org/packages/RecurrentTasks/)

Target [framework/platform moniker](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md): **`netstandard1.3`**

### Dependencies

* Microsoft.Extensions.Logging.Abstractions
* Microsoft.Extensions.DependencyInjection.Abstractions
* System.Threading
