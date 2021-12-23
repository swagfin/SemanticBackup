using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.AddTokenToHeader(GetToken());
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }
            throw new Exception(GetSimplifiedMessage(response));
        }

        public async Task<T> PostAsync<T>(string url, object data)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.AddTokenToHeader(GetToken());
            var response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(data), encoding: System.Text.Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }
            throw new Exception(GetSimplifiedMessage(response));
        }

        public async Task<T> PutAsync<T>(string url, object data)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.AddTokenToHeader(GetToken());
            var response = await client.PutAsync(url, new StringContent(JsonConvert.SerializeObject(data), encoding: System.Text.Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }
            throw new Exception(GetSimplifiedMessage(response));
        }
        public async Task<T> DeleteAsync<T>(string url)
        {
            var accessToken = GetToken();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(string.Format("{0}{1}/", ApiEndPoint, (ResourceGroups.CurrentResourceGroup == null) ? "*" : ResourceGroups.CurrentResourceGroup?.Id));
            client.AddTokenToHeader(GetToken());
            var response = await client.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(res);
            }
            throw new Exception(GetSimplifiedMessage(response));
        }
        private string GetSimplifiedMessage(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new Exception($"YOUR REQUEST/ACCESS WAS DENIED :-(. This operation requires Higher Level Access Role/Rights. If you continue to see this error, Contact Administrator to assign/elevate your Account.");
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new Exception($"UNAUTHORIZED/ SESSION TERMINATED :-(. Your account is currently unauthorized or session has expired. Please Login again to renew your Session.");
            return $"Server Responded with a {response.StatusCode} Status Code, Reason: {response.ReasonPhrase}. Please Try Again. If you continue to see this message, please Contact your Systems Administrator";
        }
        public string GetToken()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null) return null;
            var token = user.Claims.FirstOrDefault(x => x.Type == "semantic-backup-token");
            return token?.Value;
        }
    }
    public static class HttpClientExtensions
    {
        public static HttpClient AddTokenToHeader(this HttpClient cl, string token)
        {
            //int timeoutSec = 90;
            //cl.Timeout = new TimeSpan(0, 0, timeoutSec);
            string contentType = "application/json";
            cl.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            cl.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var userAgent = "d-fens HttpClient";
            cl.DefaultRequestHeaders.Add("User-Agent", userAgent);
            return cl;
        }
    }
}
