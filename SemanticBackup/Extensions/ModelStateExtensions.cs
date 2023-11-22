using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;

namespace SemanticBackup
{
    internal static class ModelStateExtensions
    {
        public static bool IsValidated(this ModelStateDictionary modelState, out string errorMessage)
        {
            errorMessage = string.Join(Environment.NewLine, modelState?.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return modelState?.IsValid ?? true;
        }
        public static void ValidateOrThrow(this ModelStateDictionary modelState)
        {
            if (!modelState.IsValidated(out string validationErrors))
                throw new Exception(validationErrors);
        }
    }
}
