using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class ResultViewModel
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    public int EventID { get; set; }

    public int ParticipantID { get; set; }

    public string EventName { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public TimeSpan? FinishTime { get; set; }

    public int? FinishingPosition { get; set; }

    public DateTime RecordedDate { get; set; }
}

public class ResultFormViewModel
{
    public int ResultID { get; set; }

    [Range(1, int.MaxValue)]
    public int EventID { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Participant entry")]
    public int EnrolmentID { get; set; }

    [Required]
    [Display(Name = "Finish time")]
    [RegularExpression(
        @"^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d$",
        ErrorMessage = "Enter a time in HH:mm:ss format, such as 00:25:30.")]
    public string FinishTime { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    [Display(Name = "Finishing position")]
    public int? FinishingPosition { get; set; }
}

public class EventResultsViewModel
{
    public EventViewModel Event { get; set; } = new();

    public List<EnrolmentViewModel> Enrolments { get; set; } = new();

    public List<ResultViewModel> Results { get; set; } = new();

    public ResultFormViewModel Form { get; set; } = new();
}