using System.ComponentModel.DataAnnotations;

namespace ProjectsWebApp.Models
{
    public class KontaktCard
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public string? Funktion { get; set; }
        public string? KontaktDatenHtml { get; set; }

        public string? ImageUrl { get; set; }

        public int DisplayOrder { get; set; }
    }
}
