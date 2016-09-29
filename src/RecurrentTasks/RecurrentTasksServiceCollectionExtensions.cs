namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using RecurrentTasks;

    public static class RecurrentTasksServiceCollectionExtensions
    {
        public static IServiceCollection AddTask<TRunnable>(this IServiceCollection services)
            where TRunnable : IRunnable
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register TRunnable in DI container, if not registered already
            if (!services.Any(x => x.ServiceType == typeof(TRunnable)))
            {
                services.AddTransient(typeof(TRunnable));
            }

            services.AddSingleton<ITask<TRunnable>, TaskRunner<TRunnable>>();

            return services;
        }
    }
}
