using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SemanticBackup.API.Extensions;
using SemanticBackup.API.SignalRHubs;
using SemanticBackup.Core;
using SemanticBackup.Core.BackgroundJobs;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.Core.ProviderServices;
using SemanticBackup.Core.ProviderServices.Implementations;
using SemanticBackup.LiteDbPersistance;

namespace SemanticBackup.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            LiteDbPersistanceOptions liteDbConfig = new LiteDbPersistanceOptions();
            Configuration.GetSection(nameof(LiteDbPersistanceOptions)).Bind(liteDbConfig);
            //Replace Variables
            liteDbConfig.ConnectionString = liteDbConfig.ConnectionString.Replace("{{env}}", this.Environment.ContentRootPath);
            //Add
            services.AddSingleton(liteDbConfig);

            PersistanceOptions persistanceOptions = new PersistanceOptions();
            Configuration.GetSection(nameof(PersistanceOptions)).Bind(persistanceOptions);
            //Replace Variables
            persistanceOptions.DefaultBackupDirectory = persistanceOptions.DefaultBackupDirectory.Replace("{{env}}", this.Environment.ContentRootPath);
            services.AddSingleton(persistanceOptions); //Configure Global Instance Reg

            //Persistance
            services.AddTransient<IDatabaseInfoPersistanceService, DatabaseInfoPersistanceService>();
            services.AddTransient<IBackupRecordPersistanceService, BackupRecordPersistanceService>();
            services.AddTransient<IBackupSchedulePersistanceService, BackupSchedulePersistanceService>();
            services.AddTransient<IResourceGroupPersistanceService, ResourceGroupPersistanceService>();
            services.AddTransient<IContentDeliveryConfigPersistanceService, ContentDeliveryConfigPersistanceService>();
            services.AddTransient<IContentDeliveryRecordPersistanceService, ContentDeliveryRecordPersistanceService>();

            //Backup Provider Engines
            services.AddTransient<ISQLServerBackupProviderService, SQLServerBackupProviderService>();
            services.AddTransient<IMySQLServerBackupProviderService, MySQLServerBackupProviderService>();

            //Background Jobs
            services.AddSingleton<IProcessorInitializable, BackupSchedulerBackgroundJob>();
            services.AddSingleton<IProcessorInitializable, BackupBackgroundJob>(); //Main Backup Thread Lunching Bots
            services.AddSingleton<IProcessorInitializable, BackupBackgroundZIPJob>(); //Zipper Thread Lunching Bots
            services.AddSingleton<IProcessorInitializable, ContentDeliverySchedulerBackgroundJob>(); //Schedules Backup for Deliveries
            services.AddSingleton<IProcessorInitializable, ContentDeliveryDispatchBackgroundJob>(); //Dispatches out saved Scheduled Jobs
            services.AddSingleton<BotsManagerBackgroundJob>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<BotsManagerBackgroundJob>()); //Carries Other Resource Group Jobs

            //Notifications
            services.AddSingleton<RecordStatusChangedHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<RecordStatusChangedHubDispatcher>());
            services.AddSingleton<IRecordStatusChangedNotifier>(svc => svc.GetRequiredService<RecordStatusChangedHubDispatcher>());

            //DASHBOARD SIGNAL DISPATCH
            services.AddSingleton<DashboardRefreshHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<DashboardRefreshHubDispatcher>());
            //Signal R and Cors
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            services.AddCors(opt =>
            {
                opt.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.SetIsOriginAllowed((x) => true);
                    builder.AllowCredentials();
                });
            });
            //Services
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Ensure Lite Db Works
            app.EnsureLiteDbDirectoryExists();
            app.EnsureBackupDirectoryExists();

            //Start Background Service
            app.UseProcessorInitializables();
            //Proceed
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            // app.UseHttpsRedirection();

            app.UseCors();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHub<RecordStatusChangedHubDispatcher>("/BackupRecordsNotify");
                endpoints.MapHub<DashboardRefreshHubDispatcher>("/DasbhoardStatistics");
            });
        }
    }
}
