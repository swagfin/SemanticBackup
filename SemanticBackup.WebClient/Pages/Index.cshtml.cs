using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SemanticBackup.WebClient.Pages
{
    public class IndexModel : PageModel
    {
        public string ApiEndPoint { get; private set; }

        public IndexModel()
        {
            ApiEndPoint = Directories.CurrentDirectory?.Url;
        }
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}
