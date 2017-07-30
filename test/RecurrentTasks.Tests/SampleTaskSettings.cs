namespace RecurrentTasks
{
    using System;
    using System.Threading;

    public class SampleTaskSettings
    {
        public readonly ManualResetEventSlim TaskRunCalled = new ManualResetEventSlim(false);

        public bool MustThrowError { get; set; } = false;

        public ManualResetEventSlim CanContinueRun = new ManualResetEventSlim(true);

        public bool MustSetIntervalToZero { get; set; } = false;

        public string FormatResult { get; set; }
    }
}
