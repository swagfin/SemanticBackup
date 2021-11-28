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
    public class BackupSchedulesController : ControllerBase
    {
        private readonly ILogger<BackupSchedulesController> _logger;
        private readonly IBackupSchedulePersistanceService _backupSchedulePersistanceService;

        public BackupSchedulesController(ILogger<BackupSchedulesController> logger, IBackupSchedulePersistanceService backupSchedulePersistanceService)
        {
            _logger = logger;
            this._backupSchedulePersistanceService = backupSchedulePersistanceService;
        }

        [HttpGet]
        public List<BackupSchedule> Get()
        {
            try
            {
                return _backupSchedulePersistanceService.GetAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<BackupSchedule>();
            }
        }
    }
}
