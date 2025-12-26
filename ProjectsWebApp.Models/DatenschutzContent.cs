using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
public class DatenschutzContent
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string SectionType { get; set; } // "Text" or "Accordion"

    public string ContentHtml { get; set; }

    public int DisplayOrder { get; set; }
}
