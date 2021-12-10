using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.ResourceGroups
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupService _resourceGroupService;

        public List<TimeZoneRecord> TimeZoneCollections { get; }
        [BindProperty]
        public ResourceGroupRequest ResourceGroupRequest { get; set; }

        public CreateModel(ILogger<IndexModel> logger, IResourceGroupService resourceGroupService, TimeZoneHelper timeZoneHelper)
        {
            this._logger = logger;
            this._resourceGroupService = resourceGroupService;
            this.TimeZoneCollections = timeZoneHelper.GetAll();
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (ResourceGroupRequest == null)
                    return Page();
                //Checks Pattern
                bool addedSuccess = await this._resourceGroupService.AddAsync(ResourceGroupRequest);
                if (addedSuccess)
                    return Redirect("/resource-groups/");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Page();
            }

        }
    }
}
