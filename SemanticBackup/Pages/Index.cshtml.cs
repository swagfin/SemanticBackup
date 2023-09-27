using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SemanticBackup.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}
