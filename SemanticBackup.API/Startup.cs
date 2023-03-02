using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SemanticBackup.API.Services;
using SemanticBackup.API.SignalRHubs;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
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
            SystemConfigOptions configOptions = new Core.SystemConfigOptions();
            Configuration.GetSection(nameof(SystemConfigOptions)).Bind(configOptions);

            //Use SemantiBackup Core Services
            services.RegisterSemanticBackupCoreServices(configOptions);
            //Notifications
            //Notifications
            services.AddSingleton<RecordStatusChangedHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<RecordStatusChangedHubDispatcher>());
            services.AddSingleton<IRecordStatusChangedNotifier>(svc => svc.GetRequiredService<RecordStatusChangedHubDispatcher>());
            services.AddSingleton<IRecordStatusChangedNotifier, StatusNotificationService>();
            //DASHBOARD SIGNAL DISPATCH
            services.AddSingleton<DashboardRefreshHubDispatcher>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<DashboardRefreshHubDispatcher>());
            //Signal R and Cors
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            //Auth
            var _options = Configuration.GetSection(nameof(SystemConfigOptions)).Get<SystemConfigOptions>();
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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //Init SemanticCore Services
            app.UseSemanticBackupCoreServices();

            app.UseCors(builder => builder
                                   .AllowAnyHeader()
                                   .AllowAnyMethod()
                                   .SetIsOriginAllowed((x) => true)
                                   .AllowCredentials()
                                  );


            app.UseStaticFiles();
            // app.UseHttpsRedirection();
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
