using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticBackup.Models.Response;
using SemanticBackup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.ResourceGroups
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IResourceGroupService _resourceGroupService;
        public string ApiEndPoint { get; }
        public ResourceGroupResponse CurrentResourceGroup { get; private set; }
        public List<ResourceGroupResponse> OtherResourceGroups { get; private set; }

        public IndexModel(ILogger<IndexModel> logger, IResourceGroupService resourceGroupService, IOptions<WebClientOptions> options)
        {
            this._logger = logger;
            this._resourceGroupService = resourceGroupService;
            ApiEndPoint = options.Value?.ApiUrl;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var allDirectories = await this._resourceGroupService.GetAllAsync();
                CurrentResourceGroup = WebClient.ResourceGroups.CurrentResourceGroup;
                if (allDirectories != null && CurrentResourceGroup != null)
                    this.OtherResourceGroups = allDirectories.Where(x => x.Id != CurrentResourceGroup.Id).ToList();
            }
            catch (Exception ex) { _logger.LogError(ex.Message); }
            return Page();
        }
    }
}
