namespace RecurrentTasks
{
    using System;
    using System.Threading;

    public class SampleTaskSettings
    {
        public ManualResetEventSlim TaskRunCalled { get; } = new ManualResetEventSlim(false);

        public bool MustThrowError { get; set; } = false;

        public ManualResetEventSlim CanContinueRun { get; } = new ManualResetEventSlim(true);

        public bool MustSetIntervalToZero { get; set; } = false;

        public bool MustRunUntilCancelled { get; set; } = false;

        public string FormatResult { get; set; }
    }
}
