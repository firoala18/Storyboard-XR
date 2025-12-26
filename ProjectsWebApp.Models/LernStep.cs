using System.ComponentModel.DataAnnotations;

namespace ProjectsWebApp.Models
{
    public class LernStep
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        // Optional image for the step
        public string? ImagePath { get; set; }

        // Order within the flow (1..n)
        public int Order { get; set; } = 1;

        // FK
        public int LernFlowId { get; set; }
        public LernFlow? Flow { get; set; }
    }
}
