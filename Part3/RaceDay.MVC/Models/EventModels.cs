using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EventViewModel
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public int EventTypeID { get; set; }
    public string? EventTypeName { get; set; }
}

public class EventTypeViewModel
{
    public int EventTypeID { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class EventFormViewModel
{
    public int EventID { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; } = DateTime.Today.AddDays(30);

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; } = 5;

    [Range(1, int.MaxValue)]
    [Display(Name = "Event type")]
    public int EventTypeID { get; set; }

    public List<EventTypeViewModel> EventTypes { get; set; } = new();
}

public class CategoryViewModel
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MaximumParticipants { get; set; }
}

public class EventDetailsViewModel
{
    public EventViewModel Event { get; set; } = new();
    public List<CategoryViewModel> Categories { get; set; } = new();
}

public class EventListViewModel
{
    public List<EventViewModel> Events { get; set; } = new();
    public List<EventTypeViewModel> EventTypes { get; set; } = new();
    public string? Search { get; set; }
    public int? EventTypeID { get; set; }
}