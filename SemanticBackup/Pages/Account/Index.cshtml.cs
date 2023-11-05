using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.Account
{
    public class IndexModel : PageModel
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public UserAccount userAccount { get; set; }

        [BindProperty]
        public bool Response { get; set; }
        public IndexModel(IUserAccountRepository userAccountRepository, ILogger<IndexModel> logger)
        {
            this._userAccountRepository = userAccountRepository;
            _logger = logger;
        }

        public IActionResult OnGetAsync()
        {
            var userEmailClaim = User.FindFirst(ClaimTypes.Email);
            if (userEmailClaim != null)
            {
                var userEmail = userEmailClaim.Value;
                var user = _userAccountRepository.GetByEmailAsync(userEmail).Result;

                if (user != null)
                {
                    userAccount = user;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            try
            {
                returnUrl = returnUrl ?? Url.Content("~/");

                var userUpdate = new UserAccount()
                {
                    UserAccountType = userAccount.UserAccountType,
                    EmailAddress = userAccount.EmailAddress,
                    FullName = userAccount.FullName,
                    Id = userAccount.Id,
                    Timezone = userAccount.Timezone,
                    TimezoneOffset = userAccount.TimezoneOffset,
                    Password = userAccount.Password,
                    LastLoginToken = userAccount.LastLoginToken,
                    LastLoginUTC = userAccount.LastLoginUTC,
                };

                var response = await _userAccountRepository.UpdateAsync(userUpdate);
                Response = response;
                if(response)
                {

                }

            }

            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

            }
            //Finnally
            return Page();

        }
    }
}
