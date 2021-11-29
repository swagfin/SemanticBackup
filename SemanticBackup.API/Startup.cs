using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SemanticBackup.API.Core;
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

            //Shared TimeZone Sync DateTime
            services.AddSingleton<SharedTimeZone>(); //Configure Global Instance Reg

            services.AddTransient<IDatabaseInfoPersistanceService, DatabaseInfoPersistanceService>();
            services.AddTransient<IBackupRecordPersistanceService, BackupRecordPersistanceService>();
            services.AddTransient<IBackupSchedulePersistanceService, BackupSchedulePersistanceService>();

            //Engines
            services.AddTransient<ISQLServerBackupProviderService, SQLServerBackupProviderService>();

            //Background Jobs
            services.AddSingleton<IProcessorInitializable, SchedulerBackgroundJob>();
            services.AddSingleton<IProcessorInitializable, BackupBackgroundJob>(); //Main Backup Thread Lunching Bots
            services.AddSingleton<IProcessorInitializable, BackupBackgroundZIPJob>(); //Zipper Thread Lunching Bots

            //Notifications
            services.AddSingleton<BackupRecordHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<BackupRecordHubDispatcher>());
            services.AddSingleton<IBackupRecordStatusChangedNotifier>(svc => svc.GetRequiredService<BackupRecordHubDispatcher>());

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
                endpoints.MapHub<BackupRecordHubDispatcher>("/BackupRecordsNotify");
            });
        }
    }
}
