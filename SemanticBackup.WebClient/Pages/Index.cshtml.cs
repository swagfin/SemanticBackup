using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace SemanticBackup.WebClient.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public string ApiEndPoint { get; private set; }

        public IndexModel(IOptions<WebClientOptions> options)
        {
            ApiEndPoint = options.Value?.ApiUrl;
        }
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}
