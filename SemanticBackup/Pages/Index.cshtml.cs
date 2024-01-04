using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
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
            return LocalRedirect($"/resource-groups/");
        }
    }
}
