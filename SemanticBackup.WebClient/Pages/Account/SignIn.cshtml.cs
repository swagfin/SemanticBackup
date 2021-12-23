using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SemanticBackup.WebClient.Models.Requests;
using SemanticBackup.WebClient.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Pages.Account
{
    public class SignInModel : PageModel
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<SignInModel> _logger;

        [BindProperty]
        public SignInRequest signInRequest { get; set; }
        public SignInModel(IHttpService httpService, ILogger<SignInModel> logger)
        {
            this._httpService = httpService;
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
                    dynamic response = await _httpService.PostAsync<dynamic>("api/useraccounts/login", signInRequest);
                    var token = (string)response.token;
                    if (string.IsNullOrWhiteSpace(token))
                        throw new Exception("Invalid Username or Password Provided");
                    //Proceeed
                    JwtSecurityToken jwttoken = new JwtSecurityTokenHandler().ReadJwtToken(token);
                    List<Claim> claims = jwttoken.Claims.ToList();
                    //Add new Role Basedd Claim
                    claims.Add(new Claim("semantic-backup-token", token));
                    //Prepare Selft Claims Identity
                    ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                    identity.AddClaims(claims);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                    AuthenticationProperties authProperties = new AuthenticationProperties
                    {
                        ExpiresUtc = jwttoken.ValidTo,
                        AllowRefresh = true,
                        IsPersistent = true,
                    };
                    //Sign In User
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(principal), authProperties);
                    await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

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
