using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

[Table("Result")]
public class Result
{
    [Key]
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    [Column(TypeName = "time")]
    public TimeSpan? FinishTime { get; set; }

    public int? FinishingPosition { get; set; }

    public DateTime RecordedDate { get; set; }

    [ForeignKey(nameof(EnrolmentID))]
    public Enrolment Enrolment { get; set; } = null!;
}