using System.ComponentModel;

public class PortalCard
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    [DisplayName("Display Order")]
    public int DisplayOrder { get; set; }

}
