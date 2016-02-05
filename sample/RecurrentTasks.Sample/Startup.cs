namespace RecurrentTasks.Sample
{
    using System;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SampleTask>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Debug;
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseIISPlatformHandler();

            app.UseMvcWithDefaultRoute();

            app.ApplicationServices.GetRequiredService<SampleTask>().Start();
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
