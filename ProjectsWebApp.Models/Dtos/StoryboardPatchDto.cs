using ProjectsWebApp.Models;

namespace ProjectsWebApp.Models.Dtos;

public class StoryboardPatchDto
{
    public byte[]? RowVersion { get; set; }

    public string? Title { get; set; }
    public string? Zielgruppe { get; set; }
    public string? Beschreibung { get; set; }
    public string? Lernziel { get; set; }
    public string? Farbpalette { get; set; }
    public TaxonomieStufe? Taxonomie { get; set; }
    public LicenseType? License { get; set; }
    public string? LicenseExtras { get; set; }
    public string? Authors { get; set; }
    public string? CoverImagePath { get; set; }
}
