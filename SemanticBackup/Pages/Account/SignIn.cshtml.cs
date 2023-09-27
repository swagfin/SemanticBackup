using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.Core.Interfaces;
using SemanticBackup.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.Account
{
    public class SignInModel : PageModel
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly ILogger<SignInModel> _logger;

        [BindProperty]
        public SignInRequest signInRequest { get; set; }
        public SignInModel(IUserAccountRepository userAccountRepository, ILogger<SignInModel> logger)
        {
            this._userAccountRepository = userAccountRepository;
            this._logger = logger;
        }
        public string ErrorResponse { get; set; } = null;
        public IActionResult OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            try
            {
                returnUrl = returnUrl ?? Url.Content("~/");
                if (string.IsNullOrWhiteSpace(signInRequest.Username))
                    ErrorResponse = "Email/Username is required to Login";
                else if (string.IsNullOrWhiteSpace(signInRequest.Password))
                    ErrorResponse = "Password is required to Login";
                else
                {
                    var userAccount = await _userAccountRepository.GetByCredentialsAsync(signInRequest.Username, signInRequest.Password);
                    if (userAccount == null)
                        return BadRequest("UserName/Email or Password is Incorrect");
                    //Update Last Login
                    userAccount.LastLoginUTC = DateTime.UtcNow;
                    userAccount.LastLoginToken = Guid.NewGuid().ToString().ToUpper();
                    bool updatedSuccess = await _userAccountRepository.UpdateLastSeenAsync(userAccount.EmailAddress, userAccount.LastLoginUTC.Value, userAccount.LastLoginToken);
                    if (!updatedSuccess)
                        _logger.LogWarning("Unable to Update Last Seen for Account");

                    List<Claim> claims = new List<Claim>
                    {
                        new Claim("userId", userAccount.Id)
                    };
                    //Prepare Selft Claims Identity
                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                    AuthenticationProperties authProperties = new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(30),
                        IsPersistent = true,
                    };
                    //Sign In User
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
                    return LocalRedirect(returnUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorResponse = $"Oops! Invalid Username or Password Provided";
            }
            //Finnally
            return Page();

        }
    }
}
