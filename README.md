# RecurrentTasks

This lightweight library allows you to run simple background tasks with specified intervals in your ASP.NET application. 

Each task is a separate `Task`, which sleeps in background for a while, wakes up, perform some job and sleeps again.

Ideal, when you don't need to run many/heavy tasks and don't want to use "big" solutions with persistence and other bells and whistles.

Written for **ASP.NET vNext** (ASP.NET 5, ASP.NET Core 1).

## Main features

* Start and Stop you task at any time;
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

### 2. Register and start your task in `Startup.cs`

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
  
And viola! Your task will run every 5 minutes (second param when calling :base constructor). Until you application alive, of course.


## Installation

Use NuGet package [RecurrentTasks](https://www.nuget.org/packages/RecurrentTasks/)

### Dependencies

* Microsoft.Extensions.Logging.Abstractions
* Microsoft.Extensions.DependencyInjection.Abstractions

## Version history

* 2.0.0 (Feb 5, 2016)
  * Class/method/property names changed. Incompatible update - major version bump.
  * New overridable methods `OnBeforeRun`, `OnAfterRunSuccess`, `OnAfterRunFail`
* 1.0.0 (Feb 4, 2016)
  * Initial release - our internal classes goes opensource