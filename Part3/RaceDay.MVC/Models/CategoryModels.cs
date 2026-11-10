using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class CategoryFormViewModel
{
    public int CategoryID { get; set; }

    public int EventID { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Maximum participants")]
    public int? MaximumParticipants { get; set; }
}

public class CategoryManageViewModel
{
    public EventViewModel Event { get; set; } = new();

    public List<CategoryViewModel> Categories { get; set; } = new();

    public CategoryFormViewModel Form { get; set; } = new();
}