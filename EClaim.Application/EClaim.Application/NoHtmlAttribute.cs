using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EClaim.Application
{
    public class NoHtmlAttribute : ValidationAttribute, IClientModelValidator
    {
        public override bool IsValid(object value)
        {
            if (value == null) return true;
            var input = value.ToString();
            return !Regex.IsMatch(input, "<.*?>");
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.Add("data-val", "true");
            context.Attributes.Add("data-val-nohtml", ErrorMessage ?? "HTML tags are not allowed.");
        }
    }
}
