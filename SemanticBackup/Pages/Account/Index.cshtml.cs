using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using System;
using System.Collections.Generic;
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

        public List<TimeZoneInfo> TimeZones { get; set; } = new List<TimeZoneInfo>();

        [BindProperty]
        public string Status { get; set; } = "update";
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
            //TimeZones = new List<TimeZoneInfo>();
            foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
            {
                TimeZones.Add(tz);
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

                if (response)
                {
                    Status = "Success";
                }
                else
                {
                    Status = "Failed";
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
