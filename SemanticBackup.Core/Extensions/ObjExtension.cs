using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.Core
{
    public static class ObjExtension
    {
        public static string GetSavingPathFromFormat(this BackupDatabaseInfo backupDatabaseInfo, string format, DateTime currentTimeUtc)
        {
            if (string.IsNullOrEmpty(format))
                return $"{backupDatabaseInfo.DatabaseName}\\{currentTimeUtc:yyyy-MM-dd}\\{backupDatabaseInfo.DatabaseName}-{currentTimeUtc:yyyy-MM-dd-HHmmss}.{backupDatabaseInfo.DatabaseType}.bak";
            //Proceed
            return format.Replace("{{database}}", backupDatabaseInfo.DatabaseName)
                                         .Replace("{{date}}", $"{currentTimeUtc:yyyy-MM-dd}")
                                         .Replace("{{datetime}}", $"UTC{currentTimeUtc:yyyy-MM-dd-HHmmssffff}")
                                         .Replace("{{databasetype}}", backupDatabaseInfo.DatabaseType);
        }

        public static ResourceGroup GetDefaultGroup(this List<ResourceGroup> resourceGroups)
        {
            return resourceGroups?.Where(x => !string.IsNullOrWhiteSpace(x.Id)).OrderByDescending(x => x.LastAccess).FirstOrDefault();
        }


        public static string GetDbConnectionString(this ResourceGroup resourceGroup, string databaseName = null)
        {
            if (!string.IsNullOrEmpty(resourceGroup.DbType) && resourceGroup.DbType.Contains("SQLSERVER"))
            {
                return string.Format("{0}{1}", $"Data Source={resourceGroup.DbServer},{resourceGroup.DbPort};Persist Security Info=True;User ID={resourceGroup.DbUsername};Password={resourceGroup.DbPassword};", string.IsNullOrWhiteSpace(databaseName) ? string.Empty : $"Initial Catalog={databaseName};");
            }
            else if (resourceGroup.DbType.Contains("MYSQL") || resourceGroup.DbType.Contains("MARIADB"))
            {
                return string.Format("{0}{1}", $"server={resourceGroup.DbServer};uid={resourceGroup.DbUsername};pwd={resourceGroup.DbPassword};port={resourceGroup.DbPort};CharSet=utf8;Connection Timeout=300;", string.IsNullOrWhiteSpace(databaseName) ? string.Empty : $"database={databaseName};");
            }
            else
                return string.Empty;
        }
        public static string GetColorCode(this ResourceGroup resourceGroup)
        {
            if (resourceGroup.DbType.Contains("SQLSERVER"))
                return "orange";
            else if (resourceGroup.DbType.Contains("MYSQL"))
                return "teal";
            else if (resourceGroup.DbType.Contains("MARIADB"))
                return "blue";
            else
                return "gray";
        }
    }
}
