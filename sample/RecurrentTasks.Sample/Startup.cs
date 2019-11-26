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
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTask<SampleTask>(o => o.AutoStart(15));

            // We want some data to persist across task runs
            // SampleTask expect this instance in .ctor(), we need to register it in DI
            services.AddSingleton<SampleTaskRunHistory>();

#if NETCOREAPP2_1
            services.AddMvc();
#else
            services.AddControllersWithViews();
#endif
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
#if NETCOREAPP2_1
            app.UseMvcWithDefaultRoute();
#else
            app.UseRouting();
            app.UseEndpoints(c => c.MapDefaultControllerRoute());
#endif
        }
    }
}
