using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupPersistance;
        public ResourceGroup CurrentResourceGroup { get; private set; }
        public List<ResourceGroup> OtherResourceGroups { get; private set; }

        public IndexModel(ILogger<IndexModel> logger, IResourceGroupRepository resourceGroupPersistance)
        {
            this._logger = logger;
            this._resourceGroupPersistance = resourceGroupPersistance;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                OtherResourceGroups = await _resourceGroupPersistance.GetAllAsync();
                Common.Shared.All = OtherResourceGroups;
                CurrentResourceGroup = Common.Shared.CurrentResourceGroup;
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
            return Page();
        }
    }
}
