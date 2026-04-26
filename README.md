# SemanticBackup

SemanticBackup is a .NET 8 backup service for SQL Server, MySQL, and MariaDB with a web dashboard, background workers, automatic scheduling, compression, and optional delivery targets (FTP, SMTP, Dropbox, Object Storage, and download links).

## What The System Does

SemanticBackup continuously runs background jobs that:

1. Read configured resources and databases from `Configs/appsettings*.json`.
2. Generate backup schedules from each database `BackupEveryHrs` value.
3. Queue and execute backups.
4. Compress backup files to `.zip` (if enabled).
5. Dispatch uploads/deliveries to enabled targets.
6. Track status in real time for the dashboard.
7. Send notification emails when backup or upload failures occur (if enabled).

## Configuration

All runtime configuration is loaded from:

- `SemanticBackup/Configs/appsettings.json`
- `SemanticBackup/Configs/appsettings.Development.json`
- `SemanticBackup/Configs/appsettings.Docker.json`

### 1. `AdminUsers`

Controls dashboard login accounts.

- Identity key is `EmailAddress`.
- At least one admin user should be configured.

### 2. `BackupConfigurations`

Array of backup resources.  
Each item defines:

- `Name`: friendly resource name
- `Type`: `SQLSERVER`, `MYSQL`, or `MARIADB`
- `ConnectionString`: database server connection
- `Databases`: list of database backup rules
  - `Name`
  - `BackupEveryHrs`
  - `FullBackup`
- `UploadTo`: enabled delivery types for this resource

### 3. `UploadConfigurations`

Global settings for each upload channel:

- `DownloadLink`
- `Ftp`
- `Smtp`
- `ObjectStorage`
- `Dropbox`

These values are used when a resource enables the matching upload type in `UploadTo`.

### 4. `NotificationConfigs`

Global failure alert settings:

- `NotifyOnBackupFailure`
- `NotifyOnUploadFailure`
- `NotifyEmailDestinations` (array of email addresses)

### 5. `SystemConfigOptions`

Core runtime options:

- `DefaultBackupDirectory`
- `BackupFileSaveFormat`
- `MaxWorkers`
- `AutoCompressToZip`
- `BackupExpiryAgeInDays`
- `ExecutionTimeoutInMinutes`
- `InDepthBackupRecordDeleteEnabled`
- SMTP system mail options used by notification service

## Example Configuration

```json
{
  "AdminUsers": [
    {
      "FullName": "Administrator",
      "EmailAddress": "admin@example.com",
      "Password": "change-me",
      "Timezone": "Africa/Nairobi",
      "TimezoneOffset": "+03:00"
    }
  ],
  "BackupConfigurations": [
    {
      "Name": "Main SQL Server",
      "Type": "SQLSERVER",
      "ConnectionString": "Server=127.0.0.1,1433;User ID=sa;Password=StrongPassword;TrustServerCertificate=True;",
      "Databases": [
        {
          "Name": "test-db",
          "BackupEveryHrs": 3,
          "FullBackup": true
        }
      ],
      "UploadTo": [ "downloadLink", "ftp", "smtp" ]
    }
  ],
  "UploadConfigurations": {
    "DownloadLink": {
      "UseShortDownloadLink": true
    },
    "Ftp": {
      "Server": "ftp.example.com",
      "Username": "ftp-user",
      "Password": "ftp-password",
      "Directory": "/backups/"
    },
    "Smtp": {
      "SMTPEnableSSL": true,
      "SMTPHost": "mail.smtp.domain.com",
      "SMTPPort": 587,
      "SMTPEmailAddress": "your.smtp@email-here.com",
      "SMTPEmailCredentials": "#your.smtp.password#",
      "SMTPDefaultSMTPFromName": "SEMANTIC SYSTEM BACKUP",
      "SMTPDestinations": "ops@example.com"
    }
  },
  "NotificationConfigs": {
    "NotifyOnBackupFailure": false,
    "NotifyOnUploadFailure": false,
    "NotifyEmailDestinations": [ "ops@example.com" ]
  },
  "SystemConfigOptions": {
    "DefaultBackupDirectory": "c:\\backups\\",
    "BackupFileSaveFormat": "{{database}}\\{{database}}-{{datetime}}.{{databasetype}}.bak",
    "MaxWorkers": 2,
    "AutoCompressToZip": true,
    "BackupExpiryAgeInDays": 7,
    "ExecutionTimeoutInMinutes": 10,
    "InDepthBackupRecordDeleteEnabled": true
  }
}
```

## Running With Docker

```sh
docker pull ghcr.io/swagfin/semantic-backup:latest
docker run -d -p 8080:8080 ghcr.io/swagfin/semantic-backup:latest
```

## Dashboard

Use the web dashboard to:

- Monitor resources and database backup activity
- Inspect backup and delivery status
- View real-time updates from SignalR streams

## Screenshots

![Dashboard](https://github.com/swagfin/SemanticBackup/blob/53acc1e03a3b6cfe6520b45d4d7cf22727a81fe3/screenshots/dashboard.PNG)
*Dashboard with backup metrics and status visualization.*

![Backup](https://github.com/swagfin/SemanticBackup/blob/53acc1e03a3b6cfe6520b45d4d7cf22727a81fe3/screenshots/backup.PNG)
*Database backup execution and tracking.*

## License

SemanticBackup is released under the [MIT License](LICENSE).
