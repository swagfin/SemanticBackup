using Microsoft.AspNetCore.Builder;
using SemanticBackup.WebClient.Models.Response;
using SemanticBackup.WebClient.Services;
using System.Collections.Generic;
using System.Linq;

namespace SemanticBackup.WebClient.Extensions
{
    public static class AppBuilderExtensions
    {
        public static void InitActiveDirectoryServices(this IApplicationBuilder builder)
        {
            var directoryStorageService = (IDirectoryStorageService)builder.ApplicationServices.GetService(typeof(IDirectoryStorageService));
            if (directoryStorageService == null)
                return;
            //Init Directory Services
            directoryStorageService.ReloadTempDirectories().GetAwaiter().GetResult();
        }

    }

}
namespace SemanticBackup.WebClient
{
    public static class Directories
    {
        public static List<ActiveDirectoryResponse> ActiveDirectories { get; set; } = new List<ActiveDirectoryResponse>();
        public static ActiveDirectoryResponse CurrentDirectory
        {
            get
            {
                return ActiveDirectories.Where(x => !string.IsNullOrWhiteSpace(x.Id)).OrderByDescending(x => x.LastAccess).FirstOrDefault();
            }
        }
    }

}