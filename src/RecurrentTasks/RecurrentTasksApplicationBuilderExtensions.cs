namespace RecurrentTasks
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public static class RecurrentTasksApplicationBuilderExtensions
    {
        public static void StartTask<TRunnable>(this IApplicationBuilder app, TimeSpan interval)
            where TRunnable : IRunnable
        {
            StartTask<TRunnable>(app, t => t.Interval = interval);
        }

        public static void StartTask<TRunnable>(this IApplicationBuilder app, TimeSpan interval, TimeSpan initialTimeout)
            where TRunnable : IRunnable
        {
            StartTask<TRunnable>(app, t => t.Interval = interval, initialTimeout);
        }

        public static void StartTask<TRunnable>(this IApplicationBuilder app, Action<ITask> setupAction)
            where TRunnable : IRunnable
        {
            StartTask<TRunnable>(app, setupAction, TimeSpan.FromSeconds(new Random().Next(10, 30)));
        }

        public static void StartTask<TRunnable>(this IApplicationBuilder app, Action<ITask> setupAction, TimeSpan initialTimeout)
            where TRunnable : IRunnable
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            var task = app.ApplicationServices.GetRequiredService<ITask<TRunnable>>();
            setupAction(task);
            task.Start(initialTimeout);
        }
    }
}
