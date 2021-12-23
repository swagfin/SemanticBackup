using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Services.Implementations
{
    public class HttpService : IHttpService
    {

        public string ApiEndPoint { get; }
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HttpService(IOptions<WebClientOptions> options, IHttpContextAccessor httpContextAccessor)
        {
            ApiEndPoint = options.Value?.ApiUrl;
            this._httpContextAccessor = httpContextAccessor;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var accessToken = GetToken();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }
            throw new Exception { };
        }

        public async Task<T> PostAsync<T>(string url, object data)
        {
            var accessToken = GetToken();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(data), encoding: System.Text.Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }

            throw new Exception { };
        }

        public async Task<T> PutAsync<T>(string url, object data)
        {
            var accessToken = GetToken();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.PutAsync(url, new StringContent(JsonConvert.SerializeObject(data), encoding: System.Text.Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }

            throw new Exception { };
        }
        public async Task<T> DeleteAsync<T>(string url)
        {
            var accessToken = GetToken();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }

            throw new Exception { };
        }
        public string GetToken()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null) return null;
            var token = user.Claims.FirstOrDefault(x => x.Type == "semantic-backup-token");
            return token?.Value;
        }

        public bool Authenticated()
        {
            var user = _httpContextAccessor.HttpContext.User;
            return user == null ? false : true;
        }

        public string GetLoggedInUserId()
        {
            var user = _httpContextAccessor.HttpContext.User;
            return user == null ? string.Empty : user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}
