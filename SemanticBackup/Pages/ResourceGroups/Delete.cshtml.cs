using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Models.Response;
using SemanticBackup.Services;
using System;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    public class DeleteModel : PageModel
    {

        private readonly ILogger<DeleteModel> _logger;
        private readonly IResourceGroupService _resourceGroupsService;
        public ResourceGroupResponse CurrentRecord { get; private set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public DeleteModel(ILogger<DeleteModel> logger, IResourceGroupService resourceGroupsService)
        {
            this._logger = logger;
            this._resourceGroupsService = resourceGroupsService;
        }


        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                ErrorMessage = null;
                if (string.IsNullOrWhiteSpace(id))
                    return Redirect("/resource-groups/");
                this.CurrentRecord = await this._resourceGroupsService.GetByIdAsync(id);
                if (this.CurrentRecord == null)
                    return Redirect("/resource-groups");
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex.Message);
                return Page();
            }
        }
        public async Task<IActionResult> OnPostAsync(string id)
        {
            try
            {
                ErrorMessage = null;
                if (string.IsNullOrWhiteSpace(id))
                    return Redirect("/resource-groups/");
                this.CurrentRecord = await this._resourceGroupsService.GetByIdAsync(id);
                if (this.CurrentRecord == null)
                    return Redirect("/resource-groups");
                //Confirm Delete
                bool success = await this._resourceGroupsService.RemoveAsync(id);
                if (!success) throw new Exception("Resource group was not deleted");
                return Redirect("/resource-groups");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex.Message);
                return Page();
            }
        }
    }
}
