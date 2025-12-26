using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProjectsWebApp.Models
{
    public class MitmachenContent
    {
        public int Id { get; set; }

        [Required]
        public string SectionType { get; set; } // E.g., "Header", "Card", "Accordion"

        [Required]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content cannot be empty")]
        public string Content { get; set; } // HTML content for rich text

        [DisplayName("Display Order")]
        public int DisplayOrder { get; set; }

    }
}
