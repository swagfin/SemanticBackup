using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;

namespace SemanticBackup
{
    internal static class ModelStateExtensions
    {
        public static bool TryValidate(this ModelStateDictionary modelState, out string errorMessage)
        {
            errorMessage = string.Join(Environment.NewLine, modelState?.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return modelState?.IsValid ?? true;
        }
    }
}
