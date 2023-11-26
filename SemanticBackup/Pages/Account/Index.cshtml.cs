using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                    TimezoneId = _systemTimeZones.FirstOrDefault(t => t.Id.Equals(existingUserInfo.Timezone)).Id
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
            try
            {
                string userId = User.GetClaimValue(ClaimTypes.PrimarySid) ?? throw new Exception("no user id provided");
                UserAccount existingUserInfo = await _userAccountRepository.GetByIdAsync(userId) ?? throw new Exception($"invalid user with id: {userId}");
                //validate
                if (!ModelState.IsValidated(out string validationErrors))
                {
                    Status = validationErrors;
                    return Page();
                }
                else if (!string.IsNullOrEmpty(UserAccountRequest.NewPassword) && UserAccountRequest.NewPassword.Length < 4)
                {
                    Status = "Password must be at list 4 characters in length";
                    return Page();
                }
                //update details
                existingUserInfo.FullName = UserAccountRequest.FullName;
                existingUserInfo.EmailAddress = UserAccountRequest.EmailAddress;
                //check password update
                existingUserInfo.Password = string.IsNullOrEmpty(UserAccountRequest.NewPassword) ? existingUserInfo.Password : UserAccountRequest.NewPassword;
                //timezone
                (string timezone, string offset) timezoneWithOffset = _systemTimeZones.FirstOrDefault(t => t.Id.Equals(UserAccountRequest.TimezoneId)).ToTimezoneWithOffset();
                //update
                existingUserInfo.Timezone = timezoneWithOffset.timezone;
                existingUserInfo.TimezoneOffset = timezoneWithOffset.offset;
                //update record
                bool updatedSuccess = await _userAccountRepository.UpdateAsync(existingUserInfo);
                //refresh jwt token with details
                if (updatedSuccess)
                    await RefreshJwtTokenAsync(existingUserInfo);
                Status = updatedSuccess ? "Success" : "Failed";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Redirect($"/account/login?redirect={"/account".UrlEncoded()}");
            }
        }

        private async Task RefreshJwtTokenAsync(UserAccount userAccount)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userAccount.FullName),
                new Claim(ClaimTypes.Email, userAccount.EmailAddress),
                new Claim(ClaimTypes.PrimarySid, userAccount.Id),
                new Claim(ClaimTypes.Hash, userAccount.LastLoginToken),
                new Claim(ClaimTypes.NameIdentifier, userAccount.EmailAddress),
                new Claim(nameof(userAccount.Timezone), userAccount.Timezone),
                new Claim(nameof(userAccount.TimezoneOffset), userAccount.TimezoneOffset),
            };
            //Prepare Selft Claims Identity
            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            AuthenticationProperties authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTime.UtcNow.AddDays(24),
                IsPersistent = true,
            };
            //Sign In User
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
        }
    }

}
