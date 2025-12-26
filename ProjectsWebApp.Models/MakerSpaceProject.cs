using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

public class MakerSpaceProject
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [ValidateNever]
    public string? Tags { get; set; } // e.g., "3D Printing, Arduino, Robotics"

    [Required]
    public string Description { get; set; }

    [Required]
    public int DisplayOrder { get; set; } = 0; // Project display order

    [Url]
    public string ProjectUrl { get; set; } // Link to external project site
    public bool Top { get; set; } = false;
    public bool Forschung { get; set; } = false;

    public bool download { get; set; } = false;
    public bool tutorial { get; set; } = false;
    public bool events { get; set; } = false;
    public bool netzwerk { get; set; } = false;
    public bool lesezeichen { get; set; } = false;
    public bool ITRecht { get; set; } = false;
    public bool Beitraege { get; set; } = false;
    public string? ImageUrl { get; set; }
}
