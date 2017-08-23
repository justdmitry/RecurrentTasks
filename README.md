# RecurrentTasks

This lightweight library allows you to run simple background tasks with specified intervals in your ASP.NET application. 

Each task is a separate `Task`, which sleeps in background for a while, wakes up, perform some job and sleeps again.

Ideal, when you don't need to run many/heavy tasks and don't want to use "big" solutions with persistence and other bells and whistles.

Written for **ASP.NET Core** (ASP.NET 5, ASP.NET vNext).

[![Build status](https://ci.appveyor.com/api/projects/status/uucaowlbcxybi4v6/branch/master?svg=true)](https://ci.appveyor.com/project/justdmitry/recurrenttasks/branch/master) 
[![NuGet](https://img.shields.io/nuget/v/RecurrentTasks.svg?maxAge=86400&style=flat)](https://www.nuget.org/packages/RecurrentTasks/) 
[![codecov](https://codecov.io/gh/justdmitry/RecurrentTasks/branch/master/graph/badge.svg)](https://codecov.io/gh/justdmitry/RecurrentTasks)

## Main features

* Start and Stop your task at any time;
* First run (after Start) is delayed at random value (10-30 sec, customizable) to prevent app freeze during statup;
* Run "immediately" (without waiting for next scheduled time);
* Change run interval while running;
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
    
    public void Run(ITask currentTask, CancellationToken cancellationToken)
    {
        // Place your code here
    }
}
```

You can add any parameters to constructor, while they are resolvable from DI container (including scope-lifetime services, because new scope is created for every task run).

### 2. Register and start your task in `Startup.cs`


```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddTask<MyFirstTask>();
    ...
}
    
public void Configure(IApplicationBuilder app, ...)
{
    ...
    app.StartTask<MyFirstTask>(TimeSpan.FromMinutes(5));
    ...
}
```

And viola! Your task will run every 5 minutes. Until you application alive, of course.

## Installation

Use NuGet package [RecurrentTasks](https://www.nuget.org/packages/RecurrentTasks/)

Target [framework/platform moniker](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md): **`netstandard1.3`**

### Dependencies

* Microsoft.AspNetCore.Http.Abstractions
* Microsoft.Extensions.Logging.Abstractions
* Microsoft.Extensions.DependencyInjection.Abstractions
