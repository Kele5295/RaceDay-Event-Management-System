using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

[Table("Enrolment")]
public class Enrolment
{
    [Key]
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }

    public int EventID { get; set; }

    public int CategoryID { get; set; }

    public DateTime EnrolmentDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Confirmed";

    [ForeignKey(nameof(ParticipantID))]
    public User Participant { get; set; } = null!;

    [ForeignKey(nameof(EventID))]
    public Event Event { get; set; } = null!;

    [ForeignKey(nameof(CategoryID))]
    public Category Category { get; set; } = null!;
}