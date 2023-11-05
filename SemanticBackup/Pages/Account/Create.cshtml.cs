using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;

namespace SemanticBackup.Pages.Account
{

    public class CreateModel : PageModel
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly ILogger<CreateModel> _logger;

        [BindProperty]
        public UserAccount userAccount { get; set; }

        [BindProperty]
        public string Status { get; set; } = "update";
           
        public CreateModel(IUserAccountRepository userAccountRepository, ILogger<CreateModel> logger)
        {
            this._userAccountRepository = userAccountRepository;
            _logger = logger;
        }
        public void OnGet()
        {
        }
    }
}
