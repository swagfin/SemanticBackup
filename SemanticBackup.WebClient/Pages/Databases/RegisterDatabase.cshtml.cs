using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.Databases
{
    public class RegisterDatabaseModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<IndexModel> _logger;

        public string ApiEndPoint { get; }
        [BindProperty]
        public BackupDatabaseRequest backupDatabaseRequest { get; set; }
        [BindProperty]
        public IEnumerable<string> DatabaseNames { get; set; }
        public string ErrorResponse { get; set; } = null;
        public RegisterDatabaseModel(IHttpService httpService, ILogger<IndexModel> logger)
        {
            this._httpService = httpService;
            this._logger = logger;
            ApiEndPoint = Directories.CurrentDirectory?.Url;
        }
        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                ErrorResponse = null;
                if (string.IsNullOrWhiteSpace(backupDatabaseRequest.DatabaseType))
                {
                    ErrorResponse = "First Select the Database Type";
                    return Page();
                }
                if (string.IsNullOrWhiteSpace(backupDatabaseRequest.Server))
                {
                    ErrorResponse = "Server Name was not provided";
                    return Page();
                }
                if (DatabaseNames == null || DatabaseNames.Count() < 1)
                {
                    ErrorResponse = "Select or add atlist one Database";
                    return Page();
                }
                else
                {
                    backupDatabaseRequest.DatabaseName = string.Join(",", DatabaseNames.Select(x => x));
                }
                //Proceed
                var url = "api/BackupDatabases/";
                var result = await _httpService.PostAsync<StatusResponseModel>(url, backupDatabaseRequest);
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorResponse = ex.Message;
                return Page();
            }

        }
    }
}
