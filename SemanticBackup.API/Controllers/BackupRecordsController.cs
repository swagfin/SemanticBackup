using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;

namespace SemanticBackup.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupRecordsController : ControllerBase
    {
        private readonly ILogger<BackupRecordsController> _logger;
        private readonly IBackupRecordPersistanceService _backupRecordPersistanceService;

        public BackupRecordsController(ILogger<BackupRecordsController> logger, IBackupRecordPersistanceService backupRecordPersistanceService)
        {
            _logger = logger;
            this._backupRecordPersistanceService = backupRecordPersistanceService;
        }

        [HttpGet]
        public List<BackupRecord> Get()
        {
            try
            {
                return _backupRecordPersistanceService.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupRecord>();
            }
        }
    }
}
