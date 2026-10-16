using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

[Table("Event")]
public class Event
{
    [Key]
    public int EventID { get; set; }

    public int OrganiserID { get; set; }

    public int EventTypeID { get; set; }

    [Required, MaxLength(120)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "date")]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Column(TypeName = "decimal(6,2)")]
    public decimal Distance { get; set; }

    [ForeignKey(nameof(OrganiserID))]
    public User Organiser { get; set; } = null!;

    [ForeignKey(nameof(EventTypeID))]
    public EventType EventType { get; set; } = null!;
}