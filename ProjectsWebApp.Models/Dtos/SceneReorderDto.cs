namespace ProjectsWebApp.Models.Dtos;

public class SceneReorderDto
{
    public int StoryboardId { get; set; }
    public List<int> SceneIds { get; set; } = new();
}
