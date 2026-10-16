using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Participants only: enter an event and select its category.
    [HttpPost("enrolments")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> CreateEnrolment(
        EnrolmentRequest request)
    {
        int participantId = HttpContext.Session.GetInt32("UserID")!.Value;

        // Keep capacity checking and insertion in one transaction.
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var raceEvent = await _context.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EventID == request.EventID);

        if (raceEvent is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        var category = await _context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CategoryID == request.CategoryID);

        if (category is null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.EventID != request.EventID)
        {
            return BadRequest(new
            {
                message = "The category does not belong to this event."
            });
        }

        if (raceEvent.EventDate.Date < DateTime.Today)
        {
            return BadRequest(new
            {
                message = "You cannot enter an event that has already passed."
            });
        }

        if (await _context.Enrolments.AnyAsync(e =>
                e.ParticipantID == participantId
                && e.EventID == request.EventID))
        {
            return Conflict(new
            {
                message = "You already have an enrolment for this event."
            });
        }

        if (category.MaximumParticipants.HasValue)
        {
            int activeEnrolments = await _context.Enrolments.CountAsync(e =>
                e.CategoryID == request.CategoryID
                && e.Status != "Cancelled");

            if (activeEnrolments >= category.MaximumParticipants.Value)
            {
                return Conflict(new { message = "This category is full." });
            }
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            EventID = request.EventID,
            CategoryID = request.CategoryID,
            Status = "Confirmed"
        };

        _context.Enrolments.Add(enrolment);

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sql
                  && (sql.Number == 2601 || sql.Number == 2627))
        {
            return Conflict(new
            {
                message = "You already have an enrolment for this event."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            enrolment.EnrolmentID,
            enrolment.ParticipantID,
            enrolment.EventID,
            enrolment.CategoryID,
            enrolment.EnrolmentDate,
            enrolment.Status
        });
    }

    // Participants only: view their own entries.
    [HttpGet("enrolments/me")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        int participantId = HttpContext.Session.GetInt32("UserID")!.Value;

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.ParticipantID == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentID,
                e.EventID,
                EventName = e.Event.EventName,
                EventDate = e.Event.EventDate,
                e.CategoryID,
                CategoryName = e.Category.CategoryName,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // Organisers only: view entries for their own event.
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var raceEvent = await _context.Events.FindAsync(eventId);

        if (raceEvent is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(403, new
            {
                message = "You can only view enrolments for your own events."
            });
        }

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.EventID == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentID,
                e.ParticipantID,
                FirstName = e.Participant.FirstName,
                LastName = e.Participant.LastName,
                e.CategoryID,
                CategoryName = e.Category.CategoryName,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // Participants only: cancel their own entry.
    [HttpDelete("enrolments/{id:int}")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _context.Enrolments.FindAsync(id);

        if (enrolment is null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        int participantId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (enrolment.ParticipantID != participantId)
        {
            return StatusCode(403, new
            {
                message = "You can only cancel your own enrolments."
            });
        }

        if (await _context.Results.AnyAsync(r => r.EnrolmentID == id))
        {
            return Conflict(new
            {
                message = "An enrolment with a recorded result cannot be cancelled."
            });
        }

        // Preserve the entry as history instead of deleting it.
        enrolment.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return NoContent();
    }
}    