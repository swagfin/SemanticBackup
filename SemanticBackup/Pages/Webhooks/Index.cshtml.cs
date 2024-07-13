using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SemanticBackup.Pages.Webhooks
{
    public class IndexModel : PageModel
    {
        public ActionResult OnGet()
        {
            return new OkObjectResult("healthy!");
        }
    }
}
