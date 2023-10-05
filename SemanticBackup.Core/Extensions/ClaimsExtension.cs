using System.Linq;
using System.Security.Claims;

namespace SemanticBackup.Core
{
    public static class ClaimsExtension
    {
        public static string GetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType)
        {
            return (claimsPrincipal?.Claims?.FirstOrDefault(x => x.Type == claimType))?.Value;
        }
        public static string GetUserTimeZoneOffset(this ClaimsPrincipal claimsPrincipal, string fallback = "00:00")
        {
            return (GetClaimValue(claimsPrincipal, "TimezoneOffset")) ?? fallback;
        }
        public static string GetUserTimeZone(this ClaimsPrincipal claimsPrincipal, string fallback = "Universal Standard Time (UTC 00:00)")
        {
            return (GetClaimValue(claimsPrincipal, "Timezone")) ?? fallback;
        }
    }
}
