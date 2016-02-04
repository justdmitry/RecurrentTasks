# RecurrentTasks

This lightweight library allows you to run simple background tasks with specified intervals in your ASP.NET application. 

Each task is a separate `Task`, which sleeps in background for a while, wakes up, perform some job and sleeps again.

Ideal, when you don't need to run many/heavy tasks and don't want to use "big" solutions with persistence and other bells and whistles.

Written for **ASP.NET vNext** (ASP.NET 5, ASP.NET Core 1).

## Usage

### 1. Create new task class

    public class MyFirstTask : TaskBase<TaskState>
    {
      public MyFirstTask(ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
          : base(loggerFactory, TimeSpan.FromMinutes(5), serviceScopeFactory)
      {
          // Nothing
      }
    
      public override string Name
      {
          get { return nameof(MyFirstTask); }
      }
    
      protected override void Run(IServiceProvider serviceProvider, TaskState state)
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
  
And viola! Your task will run every 5 minutes. Until you application alive, of course.
