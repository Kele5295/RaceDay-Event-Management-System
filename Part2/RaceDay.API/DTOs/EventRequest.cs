using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class EventRequest
{
    [Required, StringLength(120)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Distance must be between 0.01 and 9999.99.")]
    public decimal Distance { get; set; }

    [Range(1, int.MaxValue)]
    public int EventTypeID { get; set; }
}