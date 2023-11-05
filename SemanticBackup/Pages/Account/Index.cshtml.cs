using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace SemanticBackup.Pages.Account
{
    public class IndexModel : PageModel
    {
        private readonly IUserAccountRepository _userAccountRepository;

        [BindProperty]
        public UserAccount userAccount { get; set; }
        public IndexModel(IUserAccountRepository userAccountRepository)
        {
            this._userAccountRepository = userAccountRepository;
        }

        public IActionResult OnGetAsync()
        {
            var userEmailClaim = User.FindFirst(ClaimTypes.Email);
            if (userEmailClaim != null)
            {
                var userEmail = userEmailClaim.Value;
                var user =  _userAccountRepository.GetByEmailAsync(userEmail).Result;

                if (user != null)
                {
                   userAccount = user;
                }
            }

            return Page();
        }
    }
}
