using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaximumParticipants { get; set; }
}