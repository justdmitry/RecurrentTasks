namespace RecurrentTasks.Sample
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class Startup
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTask<SampleTask>();

            // We want some data to persist across task runs
            // SampleTask expect this instance in .ctor(), we need to register it in DI
            services.AddSingleton<SampleTaskRunHistory>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseMvcWithDefaultRoute();

            app.StartTask<SampleTask>(TimeSpan.FromSeconds(15));
        }
    }
}
