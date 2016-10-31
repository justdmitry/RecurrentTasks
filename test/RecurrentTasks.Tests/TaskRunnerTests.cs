namespace RecurrentTasks
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class TaskRunnerTests : IDisposable
    {
        private SampleTaskSettings settings = new SampleTaskSettings();

        private ITask sampleTask;

        public TaskRunnerTests()
        {
            var lf = new LoggerFactory();
            lf.AddConsole();

            var serviceProvider = new ServiceCollection()
                .AddTransient(_ => new SampleTask(settings))
                .BuildServiceProvider();

            sampleTask = new TaskRunner<SampleTask>(lf, serviceProvider.GetService<IServiceScopeFactory>());
            sampleTask.Interval = TimeSpan.FromSeconds(5);
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
            // must start after 1 seconds
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_CanNotStartTwice()
        {
            // must start after 1 seconds
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max (then failing)
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            // and real test - trying to start again
            var ex = Assert.Throws<InvalidOperationException>(() => sampleTask.Start(TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Task_RunAgainAndAgain()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(3);

            // must start after 1 seconds
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            // resetting event
            settings.TaskRunCalled.Reset();

            // waiting for next run - default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(3 + 1)));
        }

        [Fact]
        public void Task_CanStop()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(3);

            // must start after 1 seconds
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            settings.TaskRunCalled.Reset();
            sampleTask.Stop();

            // should NOT run again - waiting twice default interval and little more
            Assert.False(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(3 * 2 + 1)));
        }

        [Fact]
        public void Task_CanNotStopTwice()
        {
            // must start after 1 seconds
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            sampleTask.Stop();

            System.Threading.Thread.Sleep(500); // wait for real stop

            // and real test - trying to stop again
            var ex = Assert.Throws<InvalidOperationException>(() => sampleTask.Stop());
        }

        [Fact]
        public void Task_IsStarted_Works()
        {
            Assert.False(sampleTask.IsStarted);

            sampleTask.Start(TimeSpan.FromSeconds(1));

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            Assert.True(sampleTask.IsStarted);

            sampleTask.Stop();

            System.Threading.Thread.Sleep(500); // wait for real stop

            Assert.False(sampleTask.IsStarted);
        }

        [Fact]
        public void Task_IsRunningRightNow_Works()
        {
            Assert.False(sampleTask.IsRunningRightNow, "Already running... WFT???");

            settings.CanContinueRun.Reset(); // do not complete 'Run' without permission!

            sampleTask.Start(TimeSpan.FromSeconds(1));
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            Assert.True(sampleTask.IsRunningRightNow, "Oops, IsRunningRightNow is not 'true'. Something is broken!!!");

            settings.CanContinueRun.Set();

            sampleTask.Stop();

            System.Threading.Thread.Sleep(500); // wait for real stop

            Assert.False(sampleTask.IsRunningRightNow, "Ooops, IsRunningRightNow is still 'true'.... WTF???");
        }

        [Fact]
        public void Task_RunImmediately_Works()
        {
            // must start after 1 seconds
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)), "Failed to start first time");

            settings.TaskRunCalled.Reset();

            sampleTask.TryRunImmediately();

            // waiting very little time, not 'full' 5 secs
            Assert.True(settings.TaskRunCalled.Wait(1000), "Not run immediately :( ");
        }

        [Fact]
        public void Task_RunningAgainAfterException()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(3);

            settings.MustThrowError = true;
            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            settings.TaskRunCalled.Reset();

            // should run again - waiting twice default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(3 * 2 + 1)));
        }

        [Fact]
        public void Task_AfterRunFailGeneratedAfterException()
        {
            var eventGenerated = false;

            settings.MustThrowError = true;
            sampleTask.AfterRunFail += (object sender, ExceptionEventArgs e) =>
            {
                eventGenerated = true;
            }; 

            sampleTask.Start(TimeSpan.FromSeconds(1));

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(3)));

            System.Threading.Thread.Sleep(200); // wait for run cycle completed

            Assert.True(eventGenerated);
        }
    }
}
