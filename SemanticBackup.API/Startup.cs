using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SemanticBackup.API.Extensions;
using SemanticBackup.API.Services;
using SemanticBackup.API.Services.Implementations;
using SemanticBackup.Core;
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
            services.Configure<PersistanceOptions>(Configuration.GetSection(nameof(PersistanceOptions)));
            services.AddSingleton<IServerSharedRuntime, ServerSharedRuntime>();

            services.AddScoped<IDatabaseInfoPersistanceService, DatabaseInfoPersistanceService>();
            services.AddScoped<IBackupRecordPersistanceService, BackupRecordPersistanceService>();
            services.AddScoped<IBackupSchedulePersistanceService, BackupSchedulePersistanceService>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Ensure Lite Db Works
            app.EnsureLiteDbFolderExists();
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
