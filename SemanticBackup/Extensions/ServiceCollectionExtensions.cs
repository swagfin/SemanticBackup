using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Infrastructure.BackgroundJobs;
using SemanticBackup.Infrastructure.Implementations;
using System;
using System.IO;

namespace SemanticBackup.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterSemanticBackupCoreServices(this IServiceCollection services, SystemConfigOptions systemConfigOptions)
        {
            services.AddSingleton(systemConfigOptions);
            //Register Core

            //Repositories
            services.AddSingleton<IDatabaseInfoRepository, DatabaseInfoRepositoryAppSettings>();
            services.AddSingleton<IBackupRecordRepository, BackupRecordRepositoryLiteDb>();
            services.AddSingleton<IBackupScheduleRepository, BackupScheduleRepositoryAppSettings>();
            services.AddSingleton<IResourceGroupRepository, ResourceGroupRepositoryAppSettings>();
            services.AddSingleton<IContentDeliveryRecordRepository, ContentDeliveryRecordRepositoryLiteDb>();
            services.AddSingleton<IUserAccountRepository, UserAccountRepositoryAppSettings>();

            //Backup Provider Engines
            services.AddSingleton<IBackupProviderForSQLServer, BackupProviderForSQLServer>();
            services.AddSingleton<IBackupProviderForMySQLServer, BackupProviderForMySQLServer>();

            //Background Jobs
            services.AddSingleton<BackupSchedulerBackgroundJob>().AddHostedService(s => s.GetRequiredService<BackupSchedulerBackgroundJob>());
            services.AddSingleton<BackupBackgroundJob>().AddHostedService(s => s.GetRequiredService<BackupBackgroundJob>()); //Main Backup Thread Lunching Bots
            services.AddSingleton<BackupBackgroundZIPJob>().AddHostedService(s => s.GetRequiredService<BackupBackgroundZIPJob>()); //Zipper Thread Lunching Bots
            services.AddSingleton<BackupRecordDeliverySchedulerBackgroundJob>().AddHostedService(s => s.GetRequiredService<BackupRecordDeliverySchedulerBackgroundJob>()); //Schedules Backup for Deliveries
            services.AddSingleton<BackupRecordDeliveryDispatchBackgroundJob>().AddHostedService(s => s.GetRequiredService<BackupRecordDeliveryDispatchBackgroundJob>()); //Dispatches out saved Scheduled Jobs
            services.AddSingleton<BotsManagerBackgroundJob>().AddHostedService(s => s.GetRequiredService<BotsManagerBackgroundJob>()); //Carries Other Resource Group Jobs
        }

        public static void UseSemanticBackupCoreServices(this IApplicationBuilder builder)
        {

            #region Ensure Data Directories Exists
            //Data Directory
            string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);
            //Backup Directory
            string backupDirectory = ((SystemConfigOptions)builder.ApplicationServices.GetService(typeof(SystemConfigOptions)))?.DefaultBackupDirectory;
            if (!string.IsNullOrWhiteSpace(backupDirectory) && !Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);
            #endregion

        }
    }
}
