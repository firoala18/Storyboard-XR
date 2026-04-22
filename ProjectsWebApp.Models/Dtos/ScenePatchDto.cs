namespace ProjectsWebApp.Models.Dtos;

public class ScenePatchDto
{
    public byte[]? RowVersion { get; set; }
    public int? Number { get; set; }
    public string? Name { get; set; }
    public string? ImagePath { get; set; }
}
