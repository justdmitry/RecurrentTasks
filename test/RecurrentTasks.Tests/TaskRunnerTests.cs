namespace RecurrentTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
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
            sampleTask.Start(TimeSpan.Zero);

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_Cant_Start_With_Negative_FirstDelay()
        {
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => sampleTask.Start(TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public void Task_Cant_Start_With_Negative_Interval()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(-1);

            Assert.ThrowsAny<InvalidOperationException>(() => sampleTask.Start(TimeSpan.Zero));
        }

        [Fact]
        public void Task_CanNotStartTwice()
        {
            sampleTask.Start(TimeSpan.Zero);

            // waiting 2 seconds max (then failing)
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            // and real test - trying to start again
            var ex = Assert.Throws<InvalidOperationException>(() => sampleTask.Start(TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Task_Set_RunningCulture()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(1);

            sampleTask.RunningCulture = new System.Globalization.CultureInfo("en-US");

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(1)));

            Assert.Equal("123456.78", settings.FormatResult);

            // resetting event
            settings.TaskRunCalled.Reset();

            sampleTask.RunningCulture = new System.Globalization.CultureInfo("ru-RU");

            // waiting for next run - default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            Assert.Equal("123456,78", settings.FormatResult);
        }

        [Fact]
        public void Task_RunAgainAndAgain()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(1)));

            // resetting event
            settings.TaskRunCalled.Reset();

            // waiting for next run - default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_RunAgainAndAgain_When_AfterRunSuccess_ThrowsException()
        {
            var afterRunCalled = new ManualResetEventSlim();

            sampleTask.Interval = TimeSpan.FromSeconds(1);

            sampleTask.AfterRunSuccessAsync = (o, a) =>
            {
                afterRunCalled.Set();
                throw new Exception("Test exception");
            };

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(1)));

            Assert.True(afterRunCalled.Wait(TimeSpan.FromSeconds(1)));

            // resetting event
            settings.TaskRunCalled.Reset();

            // waiting for next run - default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_RunAgainAndAgain_When_AfterRunFail_ThrowsException()
        {
            var afterRunCalled = new ManualResetEventSlim();

            settings.MustThrowError = true;

            sampleTask.Interval = TimeSpan.FromSeconds(1);

            sampleTask.AfterRunFailAsync = (o, a, e) =>
            {
                afterRunCalled.Set();
                throw new Exception("Test exception");
            };

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(1)));

            Assert.True(afterRunCalled.Wait(TimeSpan.FromSeconds(1)));

            // resetting event
            settings.TaskRunCalled.Reset();

            // waiting for next run - default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_CanStop()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(1)));

            settings.TaskRunCalled.Reset();
            sampleTask.Stop();

            // should NOT run again - waiting twice default interval and little more
            Assert.False(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(3)));
        }

        [Fact]
        public void Task_CanStopByCancellationToken()
        {
            var cts = new CancellationTokenSource();

            sampleTask.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Start(TimeSpan.Zero, cts.Token);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(1)));

            settings.TaskRunCalled.Reset();
            cts.Cancel();

            // should NOT run again - waiting twice default interval and little more
            Assert.False(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(3)));
        }

        [Fact]
        public void Task_Stops_When_IntervalZero()
        {
            settings.MustSetIntervalToZero = true;

            sampleTask.Interval = TimeSpan.FromSeconds(2);

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            Thread.Sleep(100); // wait for cycles complete

            Assert.Equal(TimeSpan.Zero, sampleTask.Interval);

            Assert.False(sampleTask.IsStarted);
        }

        [Fact]
        public void Task_CanNotStopTwice()
        {
            sampleTask.Start(TimeSpan.Zero);

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

            sampleTask.Start(TimeSpan.Zero);

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

            sampleTask.Start(TimeSpan.Zero);
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
            sampleTask.Interval = TimeSpan.FromSeconds(5);

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)), "Failed to start first time");

            settings.TaskRunCalled.Reset();

            sampleTask.TryRunImmediately();

            // waiting very little time, not 'full' 5 secs
            Assert.True(settings.TaskRunCalled.Wait(1000), "Not run immediately :( ");
        }

        [Fact]
        public void Task_Cant_RunImmediately_Without_Start()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(5);

            Assert.ThrowsAny<InvalidOperationException>(() => sampleTask.TryRunImmediately());
        }

        [Fact]
        public void Task_RunningAgainAfterException()
        {
            sampleTask.Interval = TimeSpan.FromSeconds(2);

            settings.MustThrowError = true;
            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            settings.TaskRunCalled.Reset();

            // should run again - waiting twice default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Task_BeforeRunGenerated()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            sampleTask.BeforeRunAsync = (o, s) =>
            {
                eventGenerated.Set();
                return Task.FromResult(0);
            };

            sampleTask.Start(TimeSpan.Zero);

            // waiting a little, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_AfterRunSuccessGenerated()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            sampleTask.AfterRunSuccessAsync = (o, s) =>
            {
                eventGenerated.Set();
                return Task.FromResult(0);
            };

            sampleTask.Start(TimeSpan.Zero);

            // waiting a little, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_AfterRunFailGeneratedAfterException()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            settings.MustThrowError = true;
            sampleTask.AfterRunFailAsync = (s, t, e) =>
            {
                eventGenerated.Set();
                return Task.FromResult(0);
            };

            sampleTask.Start(TimeSpan.Zero);

            // waiting 2 seconds max, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));

            System.Threading.Thread.Sleep(200); // wait for run cycle completed before test dispose
        }

        [Fact]
        public void Run_Cancelled_When_Task_Stopped()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            settings.MustRunUntilCancelled = true;
            sampleTask.AfterRunSuccessAsync = (s, t) =>
            {
                eventGenerated.Set();
                return Task.FromResult(0);
            };

            sampleTask.Start(TimeSpan.Zero);

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            sampleTask.Stop();

            // waiting 2 seconds max, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));

            System.Threading.Thread.Sleep(200); // wait for run cycle completed before test dispose
        }
    }
}
