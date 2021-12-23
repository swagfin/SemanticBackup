using Microsoft.AspNetCore.Builder;
using SemanticBackup.Core;
using SemanticBackup.Core.PersistanceServices;
using SemanticBackup.LiteDbPersistance;
using System.Collections.Generic;
using System.IO;

namespace SemanticBackup.API.Extensions
{
    public static class AppBuilderExtensions
    {
        public static void EnsureLiteDbDirectoryExists(this IApplicationBuilder builder)
        {
            var liteDbConfig = (LiteDbPersistanceOptions)builder.ApplicationServices.GetService(typeof(LiteDbPersistanceOptions));
            if (liteDbConfig == null)
                return;
            //Proceed
            string directory = Path.GetDirectoryName(liteDbConfig.ConnectionString);
            if (directory != null && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            //Ensure Lite Db Exists
            var liteDbContext = (ILiteDbContext)builder.ApplicationServices.GetService(typeof(ILiteDbContext));
            if (liteDbContext == null)
                return;
        }
        public static void EnsureUserAccountsExists(this IApplicationBuilder builder)
        {
            var userService = (IUserAccountPersistanceService)builder.ApplicationServices.GetService(typeof(IUserAccountPersistanceService));
            if (userService == null)
                return;
            //User Account Service
            int count = userService.GetAllCountAsync().GetAwaiter().GetResult();
            if (count == 0)
                userService.AddOrUpdateAsync(new Core.Models.UserAccount { EmailAddress = "admin@admin.com", FullName = "Administrator", Password = "admin", UserAccountType = Core.Models.UserAccountType.ADMIN }).GetAwaiter().GetResult();

        }
        public static void EnsureBackupDirectoryExists(this IApplicationBuilder builder)
        {
            var persistanceOptions = (PersistanceOptions)builder.ApplicationServices.GetService(typeof(PersistanceOptions));
            if (persistanceOptions == null)
                return;
            //Check if Option is set EnsureDefaultBackupDirectoryExists (Docker may not be able to access e.g. c://backups but SqlCMD does)
            if (!persistanceOptions.EnsureDefaultBackupDirectoryExists)
                return;
            //Proceed
            string directory = Path.GetDirectoryName(persistanceOptions.DefaultBackupDirectory);
            if (directory != null && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        public static void UseProcessorInitializables(this IApplicationBuilder builder)
        {
            var processorService = (IEnumerable<IProcessorInitializable>)builder.ApplicationServices.GetService(typeof(IEnumerable<IProcessorInitializable>));
            if (processorService != null)
                foreach (IProcessorInitializable processor in processorService)
                    processor.Initialize();
        }
    }
}
