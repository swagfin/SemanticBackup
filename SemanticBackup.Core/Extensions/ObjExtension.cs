using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.Core
{
    public static class ObjExtension
    {
        public static string GetSavingPathFromFormat(this BackupDatabaseInfo backupDatabaseInfo, string format, DateTime currentTime)
        {
            if (string.IsNullOrEmpty(format))
                return $"{backupDatabaseInfo.DatabaseName}\\{currentTime:yyyy-MM-dd}\\{backupDatabaseInfo.DatabaseName}-{currentTime:yyyy-MM-dd-HHmmss}.{backupDatabaseInfo.DatabaseType}.bak";
            //Proceed
            return format.Replace("{{database}}", backupDatabaseInfo.DatabaseName)
                                         .Replace("{{date}}", $"{currentTime:yyyy-MM-dd}")
                                         .Replace("{{datetime}}", $"{currentTime:yyyy-MM-dd-HHmmssffff}")
                                         .Replace("{{databasetype}}", backupDatabaseInfo.DatabaseType);
        }

        public static ResourceGroup GetDefaultGroup(this List<ResourceGroup> resourceGroups)
        {
            return resourceGroups?.Where(x => !string.IsNullOrWhiteSpace(x.Id)).OrderByDescending(x => x.LastAccess).FirstOrDefault();
        }
    }
}
