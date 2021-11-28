using Microsoft.AspNetCore.Builder;
using SemanticBackup.LiteDbPersistance;
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
    }
}
