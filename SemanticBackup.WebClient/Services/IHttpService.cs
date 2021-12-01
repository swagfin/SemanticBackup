using System.Threading.Tasks;

namespace SemanticBackup.WebClient.Services
{
    public interface IHttpService
    {
        Task<T> GetAsync<T>(string url);
        Task<T> PostAsync<T>(string url, object data);
        Task<T> PutAsync<T>(string url, object data);
        Task<T> DeleteAsync<T>(string url);
        string GetToken();
    }
}
