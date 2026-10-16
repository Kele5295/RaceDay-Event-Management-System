using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/eventtypes")]
public class EventTypesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventTypesController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Public: lists available sports for event creation.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventTypes()
    {
        var eventTypes = await _context.EventTypes
            .AsNoTracking()
            .OrderBy(t => t.EventTypeID)
            .Select(t => new
            {
                t.EventTypeID,
                t.TypeName,
                t.Description
            })
            .ToListAsync();

        return Ok(eventTypes);
    }
}