using System.ComponentModel.DataAnnotations;

public class UrheberrechtContent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }  // Titel der Sektion

    [Required]
    public string SectionType { get; set; } // "Text" oder "Accordion"

    public string ContentHtml { get; set; }  // HTML-Inhalt

    public int DisplayOrder { get; set; }    // Sortierreihenfolge
}
