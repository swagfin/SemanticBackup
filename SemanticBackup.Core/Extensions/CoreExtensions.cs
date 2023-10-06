using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SemanticBackup.Core.BackgroundJobs;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Logic;
using System;
using System.Collections.Generic;
using System.IO;

namespace SemanticBackup.Core
{
    public static class CoreExtensions
    {

        public static void RegisterSemanticBackupCoreServices(this IServiceCollection services, SystemConfigOptions systemConfigOptions)
        {
            services.AddSingleton(systemConfigOptions);
            //Register Core

            //Repositories
            services.AddSingleton<IDatabaseInfoRepository, DatabaseInfoRepositoryLiteDb>();
            services.AddSingleton<IBackupRecordRepository, BackupRecordRepositoryLiteDb>();
            services.AddSingleton<IBackupScheduleRepository, BackupScheduleRepositoryLiteDb>();
            services.AddSingleton<IResourceGroupRepository, ResourceGroupRepositoryLiteDb>();
            services.AddSingleton<IContentDeliveryRecordRepository, ContentDeliveryRecordRepositoryLiteDb>();
            services.AddSingleton<IUserAccountRepository, UserAccountRepositoryLiteDb>();

            //Backup Provider Engines
            services.AddSingleton<IBackupProviderForSQLServer, BackupProviderForSQLServer>();
            services.AddSingleton<IBackupProviderForMySQLServer, BackupProviderForMySQLServer>();

            //Background Jobs
            services.AddSingleton<IProcessorInitializable, BackupSchedulerBackgroundJob>();
            services.AddSingleton<IProcessorInitializable, BackupBackgroundJob>(); //Main Backup Thread Lunching Bots
            services.AddSingleton<IProcessorInitializable, BackupBackgroundZIPJob>(); //Zipper Thread Lunching Bots
            services.AddSingleton<IProcessorInitializable, BackupRecordDeliverySchedulerBackgroundJob>(); //Schedules Backup for Deliveries
            services.AddSingleton<IProcessorInitializable, BackupRecordDeliveryDispatchBackgroundJob>(); //Dispatches out saved Scheduled Jobs
            services.AddSingleton<BotsManagerBackgroundJob>().AddSingleton<IProcessorInitializable>(svc => svc.GetRequiredService<BotsManagerBackgroundJob>()); //Carries Other Resource Group Jobs


        }

        public static void UseSemanticBackupCoreServices(this IApplicationBuilder builder)
        {

            #region Ensure Data Directories Exists
            //Data Directory
            string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);
            //Backup Directory
            var backupDirectory = ((SystemConfigOptions)builder.ApplicationServices.GetService(typeof(SystemConfigOptions)))?.DefaultBackupDirectory;
            if (!string.IsNullOrWhiteSpace(backupDirectory) && !Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);
            #endregion

            #region Init Background Services
            var processorService = (IEnumerable<IProcessorInitializable>)builder.ApplicationServices.GetService(typeof(IEnumerable<IProcessorInitializable>));
            if (processorService != null)
                foreach (IProcessorInitializable processor in processorService)
                    processor.Initialize();
            #endregion

            #region Ensure Atlist One User Exists
            var userService = (IUserAccountRepository)builder.ApplicationServices.GetService(typeof(IUserAccountRepository));
            if (userService != null)
            {
                if (userService.GetAllCountAsync().GetAwaiter().GetResult() == 0)
                    userService.AddOrUpdateAsync(new Core.Models.UserAccount { EmailAddress = "admin@admin.com", FullName = "Administrator", Password = "admin", UserAccountType = Core.Models.UserAccountType.ADMIN }).GetAwaiter().GetResult();
            }
            #endregion
        }

    }
}
