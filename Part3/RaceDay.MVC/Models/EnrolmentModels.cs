using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EnrolmentFormViewModel
{
    [Range(1, int.MaxValue)]
    public int EventID { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryID { get; set; }
}

public class EnrolmentViewModel
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class EventEnrolmentsViewModel
{
    public EventViewModel Event { get; set; } = new();

    public List<EnrolmentViewModel> Enrolments { get; set; } = new();
}