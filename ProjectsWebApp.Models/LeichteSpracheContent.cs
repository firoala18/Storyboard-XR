using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace ProjectsWebApp.Models
{
    public class LeichteSpracheContent
    {
        [Key]
        public int Id { get; set; } // Primary Key

        [Required]
        [ValidateNever]
        public string ContentHtml { get; set; } // Editable content
    }
}
