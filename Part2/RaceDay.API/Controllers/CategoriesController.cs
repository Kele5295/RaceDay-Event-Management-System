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
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Public: view the categories for an event.
    [HttpGet("events/{eventId:int}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.EventID == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryName)
            .Select(c => new
            {
                c.CategoryID,
                c.EventID,
                c.CategoryName,
                c.Description,
                c.MaximumParticipants
            })
            .ToListAsync();

        return Ok(categories);
    }

    // Organisers only: create a category for their own event.
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateCategory(
        int eventId, CategoryRequest request)
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
                message = "You can only manage your own events."
            });
        }

        string name = request.CategoryName.Trim();

        if (await _context.Categories.AnyAsync(c =>
                c.EventID == eventId && c.CategoryName == name))
        {
            return Conflict(new
            {
                message = "This event already has a category with that name."
            });
        }

        var category = new Category
        {
            EventID = eventId,
            CategoryName = name,
            Description = request.Description?.Trim(),
            MaximumParticipants = request.MaximumParticipants
        };

        _context.Categories.Add(category);

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
                message = "This event already has a category with that name."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            category.CategoryID,
            category.EventID,
            category.CategoryName,
            category.Description,
            category.MaximumParticipants
        });
    }

    // Organisers only: update a category belonging to their event.
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateCategory(
        int id, CategoryRequest request)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryID == id);

        if (category is null)
        {
            return NotFound(new { message = "Category not found." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (category.Event.OrganiserID != organiserId)
        {
            return StatusCode(403, new
            {
                message = "You can only manage your own event categories."
            });
        }

        string name = request.CategoryName.Trim();

        if (await _context.Categories.AnyAsync(c =>
                c.EventID == category.EventID
                && c.CategoryID != id
                && c.CategoryName == name))
        {
            return Conflict(new
            {
                message = "This event already has a category with that name."
            });
        }

        int activeEnrolments = await _context.Enrolments.CountAsync(e =>
            e.CategoryID == id && e.Status != "Cancelled");

        if (request.MaximumParticipants.HasValue
            && request.MaximumParticipants.Value < activeEnrolments)
        {
            return Conflict(new
            {
                message = "Capacity cannot be lower than current active enrolments."
            });
        }

        category.CategoryName = name;
        category.Description = request.Description?.Trim();
        category.MaximumParticipants = request.MaximumParticipants;

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
                message = "This event already has a category with that name."
            });
        }

        return Ok(new
        {
            category.CategoryID,
            category.EventID,
            category.CategoryName,
            category.Description,
            category.MaximumParticipants
        });
    }

    // Organisers only: delete a category with no enrolments.
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryID == id);

        if (category is null)
        {
            return NotFound(new { message = "Category not found." });
        }

        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        if (category.Event.OrganiserID != organiserId)
        {
            return StatusCode(403, new
            {
                message = "You can only manage your own event categories."
            });
        }

        if (await _context.Enrolments.AnyAsync(e => e.CategoryID == id))
        {
            return Conflict(new
            {
                message = "A category with enrolments cannot be deleted."
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}