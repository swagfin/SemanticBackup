using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Services;
using SemanticBackup.SignalRHubs;

namespace SemanticBackup
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Configure API Options
            SystemConfigOptions configOptions = new Core.SystemConfigOptions();
            Configuration.GetSection(nameof(SystemConfigOptions)).Bind(configOptions);

            //Use SemantiBackup Core Services
            services.RegisterSemanticBackupCoreServices(configOptions);

            //Notifications
            services.AddSingleton<RecordStatusChangedHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<RecordStatusChangedHubDispatcher>());
            services.AddSingleton<IRecordStatusChangedNotifier>(svc => svc.GetRequiredService<RecordStatusChangedHubDispatcher>());
            services.AddSingleton<IRecordStatusChangedNotifier, StatusNotificationService>();
            //DASHBOARD SIGNAL DISPATCH
            services.AddSingleton<DashboardRefreshHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<DashboardRefreshHubDispatcher>());

            //timezone Helper
            services.AddSingleton<TimeZoneHelper>();

            //Signal R and Cors
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(opt =>
                    {
                        ///extra configs
                    });

            services.AddHttpContextAccessor();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<RecordStatusChangedHubDispatcher>("/BackupRecordsNotify");
                endpoints.MapHub<DashboardRefreshHubDispatcher>("/DasbhoardStatistics");
            });
        }
    }
}
