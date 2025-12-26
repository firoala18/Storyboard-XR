using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class SliderItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Image is required")]
    [ValidateNever]
    public string ImageUrl { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; }

    public int DisplayOrder { get; set; }

    [NotMapped]
    [ValidateNever]
    public IFormFile? ImageFile { get; set; }

    public bool IsForVirtuellesKlassenzimmer { get; set; } = false;

}