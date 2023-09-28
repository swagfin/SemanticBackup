using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupPersistance;
        public IndexModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupPersistance)
        {
            this._logger = logger;
            this._resourceGroupPersistance = resourceGroupPersistance;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                List<ResourceGroup> resourceGroups = await _resourceGroupPersistance.GetAllAsync();
                ResourceGroup activeResourceGroup = resourceGroups.GetDefaultGroup();
                if (activeResourceGroup != null)
                {
                    return LocalRedirect($"/resource-groups/{activeResourceGroup.Id}/dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return LocalRedirect($"/resource-groups/");
        }
    }
}
