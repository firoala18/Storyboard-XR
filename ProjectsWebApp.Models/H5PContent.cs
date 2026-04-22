using System.ComponentModel.DataAnnotations;

namespace ProjectsWebApp.Models
{
    public class H5PContent
    {
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        [MaxLength(1000)]
        public string? Keywords { get; set; }

        [MaxLength(512)]
        public string? ImagePath { get; set; }

        [Required, MaxLength(512)]
        public string ContentPath { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? OriginalFileName { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public bool IsPublished { get; set; } = false;
    }
}
