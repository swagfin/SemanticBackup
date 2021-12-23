using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SemanticBackup.API.Models.Requests;
using SemanticBackup.Core.Models;
using SemanticBackup.Core.PersistanceServices;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SemanticBackup.API.Controllers
{
    [Route("{resourcegroup}/api/[controller]")]
    [ApiController]
    public class UserAccountsController : ControllerBase
    {
        private readonly ILogger<UserAccountsController> _logger;
        private readonly IUserAccountPersistanceService _userAccountPersistanceService;
        private readonly ApiConfigOptions _options;

        public UserAccountsController(ILogger<UserAccountsController> logger, IUserAccountPersistanceService userAccountPersistanceService, IOptions<ApiConfigOptions> options)
        {
            this._logger = logger;
            this._userAccountPersistanceService = userAccountPersistanceService;
            this._options = options.Value;
        }

        [HttpPost("login")]
        public async Task<ActionResult> SignInAsync(SignInRequest signInRequest)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userAccount = await _userAccountPersistanceService.GetByCredentialsAsync(signInRequest.Username, signInRequest.Password);
                    if (userAccount == null)
                        return BadRequest("UserName/Email or Password is Incorrect");
                    //Update Last Login
                    userAccount.LastLoginUTC = DateTime.UtcNow;
                    userAccount.LastLoginToken = GenerateJwTToken(userAccount);
                    bool updatedSuccess = await _userAccountPersistanceService.UpdateLastSeenAsync(userAccount.EmailAddress, userAccount.LastLoginUTC.Value, userAccount.LastLoginToken);
                    if (!updatedSuccess)
                        _logger.LogWarning("Unable to Update Last Seen for Account");
                    //Proceed Generate Token
                    return Ok(new { Token = userAccount.LastLoginToken });
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }

        }

        private string GenerateJwTToken(UserAccount user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.EmailAddress.ToString()),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.FullName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FullName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.EmailAddress),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.EmailAddress.ToString())
            };
            //Add One Roles
            claims.Add(new Claim(nameof(user.UserAccountType).ToString(), user.UserAccountType.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JWTSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddDays(Convert.ToDouble(_options.JWTExpirationInDays));

            var token = new JwtSecurityToken(
                issuer: _options.JWTIssuer,
                audience: _options.JWTAudience,
                claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
