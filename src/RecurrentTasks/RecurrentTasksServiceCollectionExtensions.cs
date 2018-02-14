namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using RecurrentTasks;

    public static class RecurrentTasksServiceCollectionExtensions
    {
        public static IServiceCollection AddTask<TRunnable>(this IServiceCollection services, ServiceLifetime runnableLifetime = ServiceLifetime.Transient)
            where TRunnable : IRunnable
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var runnableType = typeof(TRunnable);

            // Register TRunnable in DI container, if not registered already
            if (!services.Any(x => x.ServiceType == runnableType))
            {
                services.Add(new ServiceDescriptor(runnableType, runnableType, runnableLifetime));
            }

            services.AddSingleton<ITask<TRunnable>, TaskRunner<TRunnable>>();

            return services;
        }
    }
}
