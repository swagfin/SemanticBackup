using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups.LiveSignalR
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupRepository;
        private readonly IDatabaseInfoRepository _databaseInfoRepository;
        public ResourceGroup CurrentResourceGroup { get; private set; }
        public List<LiveSignalDatabaseSubscription> DatabaseSubscriptions { get; private set; } = new List<LiveSignalDatabaseSubscription>();

        public IndexModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupRepository, IDatabaseInfoRepository databaseInfoRepository)
        {
            _logger = logger;
            _resourceGroupRepository = resourceGroupRepository;
            _databaseInfoRepository = databaseInfoRepository;
        }

        public async Task<IActionResult> OnGetAsync(string resourceGroupId)
        {
            try
            {
                CurrentResourceGroup = await _resourceGroupRepository.VerifyByIdOrKeyThrowIfNotExistAsync(resourceGroupId);
                List<BackupDatabaseInfo> databases = await _databaseInfoRepository.GetAllAsync(CurrentResourceGroup.Id) ?? new List<BackupDatabaseInfo>();
                DatabaseSubscriptions = databases.Select(x => new LiveSignalDatabaseSubscription
                {
                    DatabaseId = x.Id,
                    DatabaseName = x.DatabaseName
                }).OrderBy(x => x.DatabaseName).ToList();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect("/");
            }
        }
    }

    public class LiveSignalDatabaseSubscription
    {
        public string DatabaseId { get; set; }
        public string DatabaseName { get; set; }
    }
}
