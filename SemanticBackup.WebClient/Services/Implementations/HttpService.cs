using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Services.Implementations
{
    public class HttpService : IHttpService
    {
        private readonly WebClientOptions _options;

        public HttpService(IOptions<WebClientOptions> options)
        {
            _options = options.Value;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var accessToken = GetToken();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_options.WebApiUrl);
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
            client.BaseAddress = new Uri(_options.WebApiUrl);
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
            client.BaseAddress = new Uri(_options.WebApiUrl);
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
            client.BaseAddress = new Uri(_options.WebApiUrl);
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
            var _signKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningSecret));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = "tv-analytics",
                Audience = "tv-analytics",
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(15),       //Expires after 15 minutes,
                SigningCredentials = new SigningCredentials(_signKey, SecurityAlgorithms.HmacSha256Signature),
            };

            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = jwtTokenHandler.CreateJwtSecurityToken(tokenDescriptor);
            var token = jwtTokenHandler.WriteToken(jwtToken);

            return token;
        }
    }
}
