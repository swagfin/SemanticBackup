# Semantic Database Backup

SemanticBackup is a powerful .NET API service designed to automate the backup process for supported databases including SQL Server, MariaDB, and MySQL. With this API, you can easily schedule and manage backups, ensuring the safety and integrity of your valuable data.

## Key Features

- **Background Service**: The API runs as a background service, ensuring continuous and automatic backups without user intervention.
- **Supported Databases**: Supports popular databases like SQL Server, MariaDB, and MySQL, providing flexibility for your specific needs.
- **Custom Backup Schedules**: Configure personalized backup schedules to suit your requirements.
- **Dashboard**: Monitor the backup status and manage settings through an intuitive and user-friendly dashboard.

## Deploy on Docker
```sh
docker pull ghcr.io/swagfin/semantic-backup:latest
docker run -d -p 8080:8080 ghcr.io/swagfin/semantic-backup:latest
```
## Screenshots

![Dashboard](https://github.com/swagfin/SemanticBackup/blob/53acc1e03a3b6cfe6520b45d4d7cf22727a81fe3/screenshots/dashboard.PNG)
*Screenshot of the API dashboard displaying backup status and analytics*

![Backup](https://github.com/swagfin/SemanticBackup/blob/53acc1e03a3b6cfe6520b45d4d7cf22727a81fe3/screenshots/backup.PNG)
*Screenshot of the API in action, performing a database backup.*

## Contributing

Contributions to the Database Backup API are welcome! If you would like to contribute, please follow the guidelines outlined in the contributing documentation.

## License

The SemanticBackup is released under the [MIT License](LICENSE).
