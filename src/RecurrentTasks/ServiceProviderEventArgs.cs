namespace RecurrentTasks
{
    using System;

    public class ServiceProviderEventArgs : EventArgs
    {
        public ServiceProviderEventArgs(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; protected set; }
    }
}
