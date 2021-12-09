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
            var directoryStorageService = (IResourceGroupService)builder.ApplicationServices.GetService(typeof(IResourceGroupService));
            if (directoryStorageService == null)
                return;
            //Init Directory Services
            directoryStorageService.ReloadTempResourceGroups().GetAwaiter().GetResult();
        }

    }

}
namespace SemanticBackup.WebClient
{
    public static class ResourceGroups
    {
        public static List<ResourceGroupResponse> All { get; set; } = new List<ResourceGroupResponse>();
        public static ResourceGroupResponse CurrentResourceGroup
        {
            get
            {
                return All.Where(x => !string.IsNullOrWhiteSpace(x.Id)).OrderByDescending(x => x.LastAccess).FirstOrDefault();
            }
        }
    }

}