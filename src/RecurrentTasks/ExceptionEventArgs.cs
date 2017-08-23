namespace RecurrentTasks
{
    using System;

    public class ExceptionEventArgs : ServiceProviderEventArgs
    {
        public ExceptionEventArgs(IServiceProvider serviceProvider, Exception exception)
            : base(serviceProvider)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            this.Exception = exception;
        }

        public Exception Exception { get; protected set; }
    }
}
