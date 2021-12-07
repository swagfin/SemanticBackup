using Microsoft.AspNetCore.Builder;
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
            directoryStorageService.InitDirectories();
            Directories.ActiveDirectories = directoryStorageService.GetActiveDirectories();
        }

    }

}
namespace SemanticBackup.WebClient
{
    public static class Directories
    {
        public static List<ActiveDirectory> ActiveDirectories { get; set; } = new List<ActiveDirectory>();
        public static ActiveDirectory CurrentDirectory
        {
            get
            {
                return ActiveDirectories.Where(x => !string.IsNullOrWhiteSpace(x.Id)).OrderByDescending(x => x.LastAccess).FirstOrDefault();
            }
        }
    }
}