using Microsoft.AspNetCore.Builder;
using SemanticBackup.Core;
using SemanticBackup.LiteDbPersistance;
using System.Collections.Generic;
using System.IO;

namespace SemanticBackup.API.Extensions
{
    public static class AppBuilderExtensions
    {
        public static void EnsureLiteDbFolderExists(this IApplicationBuilder builder)
        {
            var liteDbConfig = (LiteDbPersistanceOptions)builder.ApplicationServices.GetService(typeof(LiteDbPersistanceOptions));
            if (liteDbConfig == null)
                return;
            //Proceed
            string directory = Path.GetDirectoryName(liteDbConfig.ConnectionString);
            if (!Directory.Exists(directory))
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
