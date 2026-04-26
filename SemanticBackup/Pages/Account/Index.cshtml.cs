using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.Account
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserAccountRepository _userAccountRepository;
        public readonly List<TimeZoneInfo> _systemTimeZones;
        private readonly ILogger<IndexModel> _logger;
        [BindProperty]
        public UserAccountRequest UserAccountRequest { get; set; }
        public string Status { get; set; }
        public IndexModel(IUserAccountRepository userAccountRepository, ILogger<IndexModel> logger)
        {
            this._logger = logger;
            this._userAccountRepository = userAccountRepository;
            this._systemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToList();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                string userId = User.GetClaimValue(ClaimTypes.PrimarySid) ?? throw new Exception("no user id provided");
                UserAccount existingUserInfo = await _userAccountRepository.GetByIdAsync(userId) ?? throw new Exception($"invalid user with id: {userId}");
                //populate request || map object
                UserAccountRequest = new UserAccountRequest
                {
                    FullName = existingUserInfo.FullName,
                    EmailAddress = existingUserInfo.EmailAddress,
                    TimezoneId = _systemTimeZones.FirstOrDefault(t => t.Id.Equals(existingUserInfo.Timezone))?.Id ?? TimeZoneInfo.Local.Id
                };
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect($"/account/login?redirect={"/account".UrlEncoded()}");
            }
        }
        public async Task<IActionResult> OnPostAsync()
        {
            await Task.CompletedTask;
            Status = "Account settings are managed through appsettings AdminUsers and cannot be changed from UI.";
            return Page();
        }
    }

}
