namespace RecurrentTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
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

            var options = new TaskOptions<SampleTask>();
            options.FirstRunDelay = TimeSpan.Zero;

            sampleTask = new TaskRunner<SampleTask>(lf, options, serviceProvider.GetService<IServiceScopeFactory>());
            sampleTask.Options.Interval = TimeSpan.FromSeconds(5);
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
            sampleTask.Start();

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_Cant_Start_With_Negative_FirstDelay()
        {
            sampleTask.Options.FirstRunDelay = TimeSpan.FromSeconds(-1);
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => sampleTask.Start());
        }

        [Fact]
        public void Task_Cant_Start_With_Negative_Interval()
        {
            sampleTask.Options.Interval = TimeSpan.FromSeconds(-1);

            Assert.ThrowsAny<InvalidOperationException>(() => sampleTask.Start());
        }

        [Fact]
        public void Task_CanNotStartTwice()
        {
            sampleTask.Start();

            // waiting 2 seconds max (then failing)
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            // and real test - trying to start again
            var ex = Assert.Throws<InvalidOperationException>(() => sampleTask.Start());
        }

        [Fact]
        public void Task_Set_RunningCulture()
        {
            sampleTask.Options.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Options.RunCulture = new System.Globalization.CultureInfo("en-US");

            sampleTask.Start();

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(1)));

            Assert.Equal("123456.78", settings.FormatResult);

            // resetting event
            settings.TaskRunCalled.Reset();

            sampleTask.Options.RunCulture = new System.Globalization.CultureInfo("ru-RU");

            // waiting for next run - default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            Assert.Equal("123456,78", settings.FormatResult);
        }

        [Fact]
        public void Task_RunAgainAndAgain()
        {
            sampleTask.Options.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Start();

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

            sampleTask.Options.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Options.AfterRunSuccess = (o, a) =>
            {
                afterRunCalled.Set();
                throw new Exception("Test exception");
            };

            sampleTask.Start();

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

            sampleTask.Options.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Options.AfterRunFail = (o, a, e) =>
            {
                afterRunCalled.Set();
                throw new Exception("Test exception");
            };

            sampleTask.Start();

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
            sampleTask.Options.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Start();

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

            sampleTask.Options.Interval = TimeSpan.FromSeconds(1);

            sampleTask.Start(cts.Token);

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

            sampleTask.Options.Interval = TimeSpan.FromSeconds(2);

            sampleTask.Start();

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            Thread.Sleep(100); // wait for cycles complete

            Assert.Equal(TimeSpan.Zero, sampleTask.Options.Interval);

            Assert.False(sampleTask.IsStarted);
        }

        [Fact]
        public void Task_CanNotStopTwice()
        {
            sampleTask.Start();

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            sampleTask.Stop();

            System.Threading.Thread.Sleep(500); // wait for real stop

            // and real test - trying to stop again
            var ex = Assert.Throws<InvalidOperationException>(() => sampleTask.Stop());
        }

        [Fact]
        public void HostedService_CanStart()
        {
            (sampleTask as IHostedService).StartAsync(CancellationToken.None);

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void HostedService_DoNotStartWithoutAutostart()
        {
            sampleTask.Options.AutoStart = false;
            (sampleTask as IHostedService).StartAsync(CancellationToken.None);

            // waiting 2 seconds max
            Assert.False(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void HostedService_CanStop()
        {
            (sampleTask as IHostedService).StartAsync(CancellationToken.None);

            // waiting 2 seconds max, then failing
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            settings.TaskRunCalled.Reset();

            (sampleTask as IHostedService).StopAsync(CancellationToken.None);

            // should NOT run again - waiting twice default interval and little more
            Assert.False(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(3)));
        }

        [Fact]
        public void Task_IsStarted_Works()
        {
            Assert.False(sampleTask.IsStarted);

            sampleTask.Start();

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

            sampleTask.Start();
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
            sampleTask.Options.Interval = TimeSpan.FromSeconds(5);

            sampleTask.Start();

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)), "Failed to start first time");

            settings.TaskRunCalled.Reset();

            sampleTask.TryRunImmediately();

            // waiting very little time, not 'full' 5 secs
            Assert.True(settings.TaskRunCalled.Wait(1000), "Not run immediately :( ");
        }

        [Fact]
        public void Task_Cant_RunImmediately_Without_Start()
        {
            sampleTask.Options.Interval = TimeSpan.FromSeconds(5);

            Assert.ThrowsAny<InvalidOperationException>(() => sampleTask.TryRunImmediately());
        }

        [Fact]
        public void Task_RunningAgainAfterException()
        {
            sampleTask.Options.Interval = TimeSpan.FromSeconds(2);

            settings.MustThrowError = true;
            sampleTask.Start();

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            settings.TaskRunCalled.Reset();

            // should run again - waiting twice default interval and little more
            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void Task_BeforeRunGenerated()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            sampleTask.Options.BeforeRun = (o, s) =>
            {
                eventGenerated.Set();
                return Task.FromResult(true);
            };

            sampleTask.Start();

            // waiting a little, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_BeforeRunCanCancelRun()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            sampleTask.Options.BeforeRun = (o, s) =>
            {
                return Task.FromResult(false);
            };

            sampleTask.Start();

            // waiting a little
            Assert.False(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_AfterRunSuccessGenerated()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            sampleTask.Options.AfterRunSuccess = (o, s) =>
            {
                eventGenerated.Set();
                return Task.FromResult(0);
            };

            sampleTask.Start();

            // waiting a little, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Task_AfterRunFailGeneratedAfterException()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            settings.MustThrowError = true;
            sampleTask.Options.AfterRunFail = (s, t, e) =>
            {
                eventGenerated.Set();
                return Task.FromResult(0);
            };

            sampleTask.Start();

            // waiting 2 seconds max, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));

            System.Threading.Thread.Sleep(200); // wait for run cycle completed before test dispose
        }

        [Fact]
        public void Run_Cancelled_When_Task_Stopped()
        {
            var eventGenerated = new ManualResetEventSlim(false);

            settings.MustRunUntilCancelled = true;
            sampleTask.Options.AfterRunSuccess = (s, t) =>
            {
                eventGenerated.Set();
                return Task.FromResult(0);
            };

            sampleTask.Start();

            Assert.True(settings.TaskRunCalled.Wait(TimeSpan.FromSeconds(2)));

            sampleTask.Stop();

            // waiting 2 seconds max, then failing
            Assert.True(eventGenerated.Wait(TimeSpan.FromSeconds(2)));

            System.Threading.Thread.Sleep(200); // wait for run cycle completed before test dispose
        }
    }
}
