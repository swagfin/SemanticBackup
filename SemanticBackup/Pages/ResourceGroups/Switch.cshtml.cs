using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Services;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    [Authorize]
    public class SwitchModel : PageModel
    {
        private readonly ILogger<SwitchModel> _logger;
        private readonly IResourceGroupService _resourceGroupsService;

        public SwitchModel(ILogger<SwitchModel> logger, IResourceGroupService resourceGroupsService)
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
