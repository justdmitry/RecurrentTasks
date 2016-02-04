namespace RecurrentTasks
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class TaskBaseTests : IDisposable
    {
        private SampleTask sampleTask;

        public TaskBaseTests()
        {
            var lf = new LoggerFactory();
            lf.AddConsole();

            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            sampleTask = new SampleTask(
                    lf,
                    TimeSpan.FromSeconds(5),
                    serviceProvider.GetService<IServiceScopeFactory>()
                    );
        }

        public void Dispose()
        {
            if (sampleTask != null)
            {
                if (sampleTask.IsStarted)
                {
                    sampleTask.Stop();
                }
            }
        }

        [Fact]
        public void Task_CanStart()
        {
            // must start after 2 seconds
            sampleTask.Start(TimeSpan.FromSeconds(2));

            // waiting 5 seconds max, then failing
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Task_CanNotStartTwice()
        {
            // must start after 1 seconds
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 5 seconds max (then failing)
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));

            // and real test - trying to start again
            var ex = Assert.Throws<InvalidOperationException>(() => sampleTask.Start());
        }

        [Fact]
        public void Task_RunAgainAndAgain()
        {
            // must start after 2 seconds
            sampleTask.Start(TimeSpan.FromSeconds(2));

            // waiting 5 seconds max, then failing
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));

            // resetting event
            sampleTask.TaskRunCalled.Reset();

            // waiting for next run - twice default interval
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void Task_CanStop()
        {
            // must start after 2 seconds
            sampleTask.Start(TimeSpan.FromSeconds(2));

            // waiting 5 seconds max, then failing
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));

            sampleTask.TaskRunCalled.Reset();
            sampleTask.Stop();

            // should NOT run again - waiting twice default interval
            Assert.False(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void Task_CanNotStopTwice()
        {
            // must start after 2 seconds
            sampleTask.Start(TimeSpan.FromSeconds(2));

            // waiting 5 seconds max, then failing
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));

            sampleTask.Stop();

            System.Threading.Thread.Sleep(500); // wait for real stop

            // and real test - trying to stop again
            var ex = Assert.Throws<InvalidOperationException>(() => sampleTask.Stop());
        }

        [Fact]
        public void Task_IsStarted_Works()
        {
            Assert.False(sampleTask.IsStarted);

            sampleTask.Start(TimeSpan.FromSeconds(2));

            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));

            Assert.True(sampleTask.IsStarted);

            sampleTask.Stop();

            System.Threading.Thread.Sleep(500); // wait for real stop

            Assert.False(sampleTask.IsStarted);
        }

        [Fact]
        public void Task_IsRunningRightNow_Works()
        {
            Assert.False(sampleTask.IsRunningRightNow, "Already running... WFT???");

            sampleTask.CanContinueRun.Reset(); // do not complete 'Run' without permission!

            sampleTask.Start(TimeSpan.FromSeconds(2));
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));

            Assert.True(sampleTask.IsRunningRightNow, "Oops, IsRunningRightNow is not 'true'. Something is broken!!!");

            sampleTask.CanContinueRun.Set();

            sampleTask.Stop();

            System.Threading.Thread.Sleep(500); // wait for real stop

            Assert.False(sampleTask.IsRunningRightNow, "Ooops, IsRunningRightNow is still 'true'.... WTF???");
        }

        [Fact]
        public void Task_RunImmediately_Works()
        {
            // must start after 2 seconds
            sampleTask.Start(TimeSpan.FromSeconds(2));

            // waiting 5 seconds max, then failing
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)), "Failed to start first time");

            sampleTask.TaskRunCalled.Reset();

            sampleTask.TryRunImmediately();

            // waiting very little time, not 'full' 5 secs
            Assert.True(sampleTask.TaskRunCalled.Wait(1000), "Not run immediately :( ");
        }

        [Fact]
        public void Task_RunningAgainAfterException()
        {
            sampleTask.MustThrowError = true;
            sampleTask.Start(TimeSpan.FromSeconds(2));

            // waiting 5 seconds max, then failing
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));

            sampleTask.TaskRunCalled.Reset();

            // should run again - waiting twice default interval
            Assert.True(sampleTask.TaskRunCalled.Wait(TimeSpan.FromSeconds(10)));
        }
    }
}
