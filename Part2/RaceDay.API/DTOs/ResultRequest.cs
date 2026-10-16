using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class ResultRequest
{
    [Required]
    [RegularExpression(
        @"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$",
        ErrorMessage = "Finish time must use HH:mm:ss, for example 00:48:32.")]
    public string FinishTime { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int? FinishingPosition { get; set; }
}