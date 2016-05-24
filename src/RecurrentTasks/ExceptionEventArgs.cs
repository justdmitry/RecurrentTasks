namespace RecurrentTasks
{
    using System;

    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception exception)
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
