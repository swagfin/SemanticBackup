using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SemanticBackup.API.Core;
using SemanticBackup.API.Extensions;
using SemanticBackup.Core;
using SemanticBackup.Core.BackgroundJobs;
using SemanticBackup.Core.BackupServices;
using SemanticBackup.Core.BackupServices.Implementations;
using SemanticBackup.Core.PersistanceServices;
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
            //Proceed
            services.AddSingleton(liteDbConfig);
            services.AddSingleton<PersistanceOptions>(); //Configure Global Instance Reg
            services.AddSingleton<SharedTimeZone>(); //Configure Global Instance Reg

            services.AddTransient<IDatabaseInfoPersistanceService, DatabaseInfoPersistanceService>();
            services.AddTransient<IBackupRecordPersistanceService, BackupRecordPersistanceService>();
            services.AddTransient<IBackupSchedulePersistanceService, BackupSchedulePersistanceService>();

            //Engines
            services.AddTransient<ISQLServerBackupProviderService, SQLServerBackupProviderService>();

            //Background Jobs
            services.AddSingleton<IProcessorInitializable, SchedulerBackgroundJob>();
            services.AddSingleton<IProcessorInitializable, BackupBackgroundJob>(); //Main Backup Thread Lunching Bots
            //Services
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Ensure Lite Db Works
            app.EnsureLiteDbFolderExists();
            //Start Background Service
            app.UseProcessorInitializables();
            //Proceed
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
