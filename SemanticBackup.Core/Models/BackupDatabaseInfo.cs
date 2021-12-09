﻿using System;
using System.ComponentModel.DataAnnotations;

namespace SemanticBackup.Core.Models
{
    public class BackupDatabaseInfo
    {
        [Required, Key]
        public string Id { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string ActiveDirectoryId { get; set; }
        public string Name { get { return $"{DatabaseName} on {Server}"; } }
        public string Description { get; set; }
        [Required]
        public string Server { get; set; } = "127.0.0.1";
        [Required]
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 0;
        [Required]
        public string DatabaseName { get; set; }
        [Required]
        public string DatabaseType { get; set; } = BackupDatabaseInfoDbType.SQLSERVER2019.ToString();
        public string DatabaseConnectionString
        {
            get
            {
                if (!string.IsNullOrEmpty(DatabaseType) && DatabaseType.Contains("SQLSERVER"))
                    return $"Data Source={Server},{Port};Initial Catalog={DatabaseName};Persist Security Info=True;User ID={Username};Password={Password};";
                else if (DatabaseType.Contains("MYSQL") || DatabaseType.Contains("MARIADB"))
                    return $"server={Server};uid={Username};pwd={Password};database={DatabaseName};port={Port};CharSet=utf8;Connection Timeout=300";
                else
                    return null;
            }
        }
        public DateTime DateRegistered { get; set; } = DateTime.Now;
        public int BackupExpiryAgeInDays { get; set; } = 7; //Default 7 Days
    }
    public enum BackupDatabaseInfoDbType
    {
        SQLSERVER2012, SQLSERVER2014, SQLSERVER2019, MARIADBDATABASE, MYSQLDATABASE
    }
}
