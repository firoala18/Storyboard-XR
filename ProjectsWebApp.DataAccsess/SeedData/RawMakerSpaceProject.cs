namespace ProjectsWebApp.SeedData
{
    // SeedData/JsonModels.cs
    public record RawMakerSpaceProject(
        int Id,
        int Verlauf,          // becomes DisplayOrder
        string Title,
        string Tags,
        bool Top,
        string ProjectUrl);

    public record Root(List<RawMakerSpaceProject> Data);

}
