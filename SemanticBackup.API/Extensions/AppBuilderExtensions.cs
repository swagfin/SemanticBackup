using Microsoft.AspNetCore.Builder;
using SemanticBackup.Core;
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
