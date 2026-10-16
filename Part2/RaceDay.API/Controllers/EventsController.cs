using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Security;
using RaceEvent = RaceDay.API.Models.Event;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Public: browse all events.
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventID,
                e.OrganiserID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventTypeID,
                EventTypeName = e.EventType.TypeName
            })
            .ToListAsync();

        return Ok(events);
    }

    // Public: view one event.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var raceEvent = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(e => new
            {
                e.EventID,
                e.OrganiserID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventTypeID,
                EventTypeName = e.EventType.TypeName
            })
            .SingleOrDefaultAsync();

        if (raceEvent is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(raceEvent);
    }

    // Organisers only: create an event owned by the logged-in user.
    [HttpPost]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        if (!await _context.EventTypes
                .AnyAsync(t => t.EventTypeID == request.EventTypeID))
        {
            return BadRequest(new { message = "Invalid event type." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        var raceEvent = new RaceEvent
        {
            OrganiserID = organiserId,
            EventName = request.EventName.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate!.Value.Date,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventTypeID = request.EventTypeID
        };

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = raceEvent.EventID },
            new
            {
                raceEvent.EventID,
                raceEvent.OrganiserID,
                raceEvent.EventName,
                raceEvent.Description,
                raceEvent.EventDate,
                raceEvent.Location,
                raceEvent.Distance,
                raceEvent.EventTypeID
            });
    }

    // Organisers only: update an event they own.
    [HttpPut("{id:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateEvent(
        int id, EventRequest request)
    {
        var raceEvent = await _context.Events.FindAsync(id);

        if (raceEvent is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(403, new
            {
                message = "You can only manage your own events."
            });
        }

        if (!await _context.EventTypes
                .AnyAsync(t => t.EventTypeID == request.EventTypeID))
        {
            return BadRequest(new { message = "Invalid event type." });
        }

        raceEvent.EventName = request.EventName.Trim();
        raceEvent.Description = request.Description?.Trim();
        raceEvent.EventDate = request.EventDate!.Value.Date;
        raceEvent.Location = request.Location.Trim();
        raceEvent.Distance = request.Distance;
        raceEvent.EventTypeID = request.EventTypeID;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            raceEvent.EventID,
            raceEvent.OrganiserID,
            raceEvent.EventName,
            raceEvent.Description,
            raceEvent.EventDate,
            raceEvent.Location,
            raceEvent.Distance,
            raceEvent.EventTypeID
        });
    }

    // Organisers only: safely delete an event they own.
    [HttpDelete("{id:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var raceEvent = await _context.Events.FindAsync(id);

        if (raceEvent is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(403, new
            {
                message = "You can only manage your own events."
            });
        }

        if (await _context.Enrolments.AnyAsync(e => e.EventID == id))
        {
            return Conflict(new
            {
                message = "An event with enrolments cannot be deleted."
            });
        }

        // Remove unused categories before deleting their event.
        var categories = await _context.Categories
            .Where(c => c.EventID == id)
            .ToListAsync();

        _context.Categories.RemoveRange(categories);
        _context.Events.Remove(raceEvent);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}