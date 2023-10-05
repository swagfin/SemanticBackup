using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    [Authorize]
    public class SwitchModel : PageModel
    {
        private readonly ILogger<SwitchModel> _logger;
        private readonly IResourceGroupRepository _resourceGroupsService;

        public SwitchModel(ILogger<SwitchModel> logger, IResourceGroupRepository resourceGroupsService)
        {
            this._logger = logger;
            this._resourceGroupsService = resourceGroupsService;
        }
        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Redirect("/resource-groups/");
                bool switchedSuccess = await this._resourceGroupsService.SwitchAsync(id);
                if (switchedSuccess)
                    return Redirect("/");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message); return Page();
            }
        }
    }
}
