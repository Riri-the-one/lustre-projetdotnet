using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProjetDotNet.Models.DTOs;
public class ProductDTO
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string? ProductName { get; set; }

    [Required]
    [MaxLength(40)]
    public string? BrandName { get; set; }
    [Required]
    [Range(typeof(decimal), "0.01", "999999.99")]
    public decimal Price { get; set; }
    public string? Image { get; set; }
    [Required]
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
    public IFormFile? ImageFile { get; set; }
    public IEnumerable<SelectListItem>? CategoryList { get; set; }
}
