using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.Databases
{
    public class RegisterDatabaseModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public BackupDatabaseRequest backupDatabaseRequest { get; set; }
        public RegisterDatabaseModel(IHttpService httpService, ILogger<IndexModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
        }
        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var url = "api/BackupDatabases/";
            var result = await _httpService.PostAsync<StatusResponseModel>(url, backupDatabaseRequest);
            return RedirectToPage("Index");
        }
    }
}
