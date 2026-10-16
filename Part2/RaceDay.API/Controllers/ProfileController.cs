using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ProfileController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Returns the logged-in user's own profile.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        int? userId = HttpContext.Session.GetInt32("UserID");

        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.UserID == userId);

        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "Please log in again." });
        }

        return Ok(new
        {
            user.UserID,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.PhoneNumber
        });
    }

    // Updates only the logged-in user's own profile.
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequest request)
    {
        int? userId = HttpContext.Session.GetInt32("UserID");

        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.UserID == userId);

        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "Please log in again." });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Profile updated successfully.",
            user.UserID,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.PhoneNumber
        });
    }
}