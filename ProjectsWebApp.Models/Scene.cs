using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.Models
{
    public class Scene
    {
        public int Id { get; set; }

        public int StoryboardId { get; set; }
        public Storyboard? Storyboard { get; set; }

        // Scene order: 1, 2, 3...
        [Range(1, 9999)]
        public int Number { get; set; } = 1;

        // Optional name: "1. Szene" / "Intro"
        [MaxLength(200)]
        public string? Name { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public List<Marker> Markers { get; set; } = new();
    }
}
