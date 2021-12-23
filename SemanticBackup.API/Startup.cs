using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SemanticBackup.API.Extensions;
using SemanticBackup.API.Services;
using SemanticBackup.API.SignalRHubs;
using SemanticBackup.Core;
using SemanticBackup.Core.BackgroundJobs;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.Core.ProviderServices;
using SemanticBackup.Core.ProviderServices.Implementations;
using SemanticBackup.LiteDbPersistance;
using System;
using System.Text;

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

            //Configure API Options
            services.Configure<ApiConfigOptions>(Configuration.GetSection(nameof(ApiConfigOptions)));

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

            //Register Database Context
            services.AddSingleton<ILiteDbContext, LiteDbContext>();

            //Persistance
            services.AddScoped<IDatabaseInfoPersistanceService, DatabaseInfoPersistanceService>();
            services.AddScoped<IBackupRecordPersistanceService, BackupRecordPersistanceService>();
            services.AddScoped<IBackupSchedulePersistanceService, BackupSchedulePersistanceService>();
            services.AddScoped<IResourceGroupPersistanceService, ResourceGroupPersistanceService>();
            services.AddScoped<IContentDeliveryConfigPersistanceService, ContentDeliveryConfigPersistanceService>();
            services.AddScoped<IContentDeliveryRecordPersistanceService, ContentDeliveryRecordPersistanceService>();
            services.AddScoped<IUserAccountPersistanceService, UserAccountPersistanceService>();

            //Backup Provider Engines
            services.AddScoped<ISQLServerBackupProviderService, SQLServerBackupProviderService>();
            services.AddScoped<IMySQLServerBackupProviderService, MySQLServerBackupProviderService>();

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
            //Email Notify
            services.AddSingleton<IRecordStatusChangedNotifier, StatusNotificationService>();

            //DASHBOARD SIGNAL DISPATCH
            services.AddSingleton<DashboardRefreshHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<DashboardRefreshHubDispatcher>());
            //Signal R and Cors
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            //Auth
            var _options = Configuration.GetSection(nameof(ApiConfigOptions)).Get<ApiConfigOptions>();
            services
                .AddAuthorization()
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = _options.JWTIssuer,
                        ValidAudience = _options.JWTAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JWTSecret)),
                        ClockSkew = TimeSpan.Zero
                    };
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
            app.UseRouting();

            app.UseCors();
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
