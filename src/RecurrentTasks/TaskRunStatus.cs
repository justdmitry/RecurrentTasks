namespace RecurrentTasks
{
    using System;

    public class TaskRunStatus
    {
        public TaskRunResult LastResult { get; set; }

        public DateTimeOffset LastRunTime { get; set; }

        public DateTimeOffset LastSuccessTime { get; set; }

        public DateTimeOffset FirstFailTime { get; set; }

        public int FailsCount { get; set; }

        public Exception? LastException { get; set; }

        public DateTimeOffset NextRunTime { get; set; }
    }
}
