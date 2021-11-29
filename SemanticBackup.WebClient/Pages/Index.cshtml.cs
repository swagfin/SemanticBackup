using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace SemanticBackup.WebClient.Pages
{
    public class IndexModel : PageModel
    {
        private readonly WebClientOptions _options;

        public string ApiEndPoint { get; }
        public IndexModel(IOptions<WebClientOptions> options)
        {
            _options = options.Value;
            ApiEndPoint = options.Value.WebApiUrl;
        }

        public void OnGet()
        {
        }
    }
}
