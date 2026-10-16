using System.Globalization;
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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Organisers only: record a result for their own event.
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateResult(
        int enrolmentId, ResultRequest request)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .SingleOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment is null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (enrolment.Event.OrganiserID != organiserId)
        {
            return StatusCode(403, new
            {
                message = "You can only record results for your own events."
            });
        }

        if (enrolment.Status != "Confirmed")
        {
            return BadRequest(new
            {
                message = "Results require a confirmed enrolment."
            });
        }

        if (enrolment.Event.EventDate.Date > DateTime.Today)
        {
            return BadRequest(new
            {
                message = "Results cannot be recorded before the event date."
            });
        }

        if (await _context.Results
                .AnyAsync(r => r.EnrolmentID == enrolmentId))
        {
            return Conflict(new
            {
                message = "This enrolment already has a result."
            });
        }

        if (!TimeSpan.TryParseExact(
                request.FinishTime,
                @"hh\:mm\:ss",
                CultureInfo.InvariantCulture,
                out var finishTime))
        {
            return BadRequest(new
            {
                message = "Finish time must use HH:mm:ss."
            });
        }

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = finishTime,
            FinishingPosition = request.FinishingPosition
        };

        _context.Results.Add(result);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sql
                  && (sql.Number == 2601 || sql.Number == 2627))
        {
            return Conflict(new
            {
                message = "This enrolment already has a result."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.FinishingPosition,
            result.RecordedDate
        });
    }

    // Organisers only: update a result for their own event.
    [HttpPut("results/{id:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateResult(
        int id, ResultRequest request)
    {
        var result = await _context.Results
            .Include(r => r.Enrolment)
            .ThenInclude(e => e.Event)
            .SingleOrDefaultAsync(r => r.ResultID == id);

        if (result is null)
        {
            return NotFound(new { message = "Result not found." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (result.Enrolment.Event.OrganiserID != organiserId)
        {
            return StatusCode(403, new
            {
                message = "You can only update results for your own events."
            });
        }

        if (result.Enrolment.Status != "Confirmed")
        {
            return BadRequest(new
            {
                message = "Results require a confirmed enrolment."
            });
        }

        if (result.Enrolment.Event.EventDate.Date > DateTime.Today)
        {
            return BadRequest(new
            {
                message = "Results cannot be recorded before the event date."
            });
        }

        if (!TimeSpan.TryParseExact(
                request.FinishTime,
                @"hh\:mm\:ss",
                CultureInfo.InvariantCulture,
                out var finishTime))
        {
            return BadRequest(new
            {
                message = "Finish time must use HH:mm:ss."
            });
        }

        result.FinishTime = finishTime;
        result.FinishingPosition = request.FinishingPosition;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.FinishingPosition,
            result.RecordedDate
        });
    }

    // Participants only: view their own results.
    [HttpGet("results/me")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        int participantId = HttpContext.Session.GetInt32("UserID")!.Value;

        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment.ParticipantID == participantId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .Select(r => new
            {
                r.ResultID,
                r.EnrolmentID,
                r.Enrolment.EventID,
                EventName = r.Enrolment.Event.EventName,
                EventDate = r.Enrolment.Event.EventDate,
                CategoryName = r.Enrolment.Category.CategoryName,
                r.FinishTime,
                r.FinishingPosition,
                r.RecordedDate
            })
            .ToListAsync();

        return Ok(results);
    }

    // Organisers only: view results for their own event.
    [HttpGet("events/{eventId:int}/results")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetEventResults(int eventId)
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
                message = "You can only view results for your own events."
            });
        }

        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment.EventID == eventId)
            .OrderBy(r => r.FinishingPosition)
            .Select(r => new
            {
                r.ResultID,
                r.EnrolmentID,
                r.Enrolment.ParticipantID,
                FirstName = r.Enrolment.Participant.FirstName,
                LastName = r.Enrolment.Participant.LastName,
                CategoryName = r.Enrolment.Category.CategoryName,
                r.FinishTime,
                r.FinishingPosition,
                r.RecordedDate
            })
            .ToListAsync();

        return Ok(results);
    }
}