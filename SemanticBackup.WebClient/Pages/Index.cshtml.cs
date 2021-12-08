using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SemanticBackup.WebClient.Pages
{
    public class IndexModel : PageModel
    {
        public string ApiEndPoint { get; private set; }

        public IActionResult OnGet()
        {
            var currentDirectory = Directories.CurrentDirectory;
            if (currentDirectory == null)
                return Redirect("/managed-directories/notify-create");
            ApiEndPoint = currentDirectory?.Url;
            return Page();
        }
    }
}
