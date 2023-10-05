using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace SemanticBackup.Pages.Account
{
    [Authorize]
    public class SignOutModel : PageModel
    {
        public async Task<ActionResult> OnGetAsync()
        {
            try
            {
                await HttpContext.SignOutAsync();
            }
            catch { }
            return LocalRedirect("/account/login");
        }
    }
}
