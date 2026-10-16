using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

[Table("EventType")]
public class EventType
{
    [Key]
    public int EventTypeID { get; set; }

    [Required, MaxLength(50)]
    public string TypeName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}