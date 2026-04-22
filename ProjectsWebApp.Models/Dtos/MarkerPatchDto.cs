using ProjectsWebApp.Models;

namespace ProjectsWebApp.Models.Dtos;

public class MarkerPatchDto
{
    public byte[]? RowVersion { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public int? Number { get; set; }
    public string? ColorHex { get; set; }
    public string? Description { get; set; }
    public string? Ziel { get; set; }
    public string? Datenablage { get; set; }
    public string? Quellen { get; set; }
    public string? PromptIdee { get; set; }
    public string? Reflexion { get; set; }
    public string? Model { get; set; }
    public TaxonomieStufe? Taxonomie { get; set; }
}
