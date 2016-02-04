namespace RecurrentTasks
{
    using System;

    public class TaskStatus
    {
        public TaskRunResult LastRunResult { get; set; }

        public DateTimeOffset LastRunTime { get; set; }

        public DateTimeOffset LastSuccessTime { get; set; }

        public DateTimeOffset FirstFail { get; set; }

        public int FailsCount { get; set; }

        public Exception LastException { get; set; }

        public DateTimeOffset NextRunTime { get; set; }
    }
}
