using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;

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

        public IActionResult OnGetAsync()
        {
            return LocalRedirect($"/resource-groups/");
        }
    }
}
