using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace SemanticBackup.WebClient.Pages.Databases
{
    public class IndexModel : PageModel
    {
        public string ApiEndPoint { get; }
        public IndexModel(IOptions<WebClientOptions> options)
        {
            ApiEndPoint = options.Value.WebApiUrl;
        }

        public void OnGet()
        {
        }
    }
}
