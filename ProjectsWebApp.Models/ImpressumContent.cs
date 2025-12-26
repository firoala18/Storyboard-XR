using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
public class ImpressumContent
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string SectionType { get; set; } // "Text" or "Accordion"

    [Required(ErrorMessage = "Content cannot be empty")]

    public string ContentHtml { get; set; }
    public int DisplayOrder { get; set; }
}
