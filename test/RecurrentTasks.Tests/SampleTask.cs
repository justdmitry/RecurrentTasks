namespace RecurrentTasks
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    
    public class SampleTask : TaskBase<TaskRunStatus>
    {
        public readonly ManualResetEventSlim TaskRunCalled = new ManualResetEventSlim(false);

        public bool MustThrowError { get; set; } = false;

        public ManualResetEventSlim CanContinueRun = new ManualResetEventSlim(true);

        public SampleTask(ILoggerFactory loggerFactory, TimeSpan interval, IServiceScopeFactory serviceScopeFactory)
            : base(loggerFactory, interval, serviceScopeFactory)
        {
            // Nothing
        }

        protected override void Run(IServiceProvider serviceProvider, TaskRunStatus runStatus)
        {
            TaskRunCalled.Set();
            if (MustThrowError)
            {
                throw new Exception("You asked - I throw");
            }
            if (!CanContinueRun.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new Exception("CanContinueRun not set during 10 seconds. Something wrong with test...");
            }
        }
    }
}
