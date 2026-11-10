using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class EventsController(RaceDayApiClient api) : Controller
{
    private bool IsOrganiser =>
        HttpContext.Session.GetInt32("UserID").HasValue
        && HttpContext.Session.GetString("Role") == "Organiser";

    private IActionResult AccessDenied()
    {
        if (!HttpContext.Session.GetInt32("UserID").HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        return StatusCode(403, "Only organisers can manage events.");
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, int? eventTypeID)
    {
        var response = await api.SendAsync<List<EventViewModel>>(
            HttpMethod.Get, "api/events");

        var types = await api.SendAsync<List<EventTypeViewModel>>(
            HttpMethod.Get, "api/eventtypes");

        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        var events = response.Data ?? new List<EventViewModel>();

        events = events
            .Where(e => e.EventDate.Date >= DateTime.Today)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                e.EventName.Contains(
                    search, StringComparison.OrdinalIgnoreCase)
                || e.Location.Contains(
                    search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (eventTypeID.HasValue && eventTypeID > 0)
        {
            events = events
                .Where(e => e.EventTypeID == eventTypeID)
                .ToList();
        }

        return View(new EventListViewModel
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            EventTypes = types.Data ?? new(),
            Search = search,
            EventTypeID = eventTypeID
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var response = await api.SendAsync<EventViewModel>(
            HttpMethod.Get, $"api/events/{id}");

        if (!response.Success || response.Data is null)
        {
            TempData["Error"] = response.Error;
            return RedirectToAction(nameof(Index));
        }

        var categories = await api.SendAsync<List<CategoryViewModel>>(
            HttpMethod.Get, $"api/events/{id}/categories");

        if (!categories.Success)
        {
            TempData["Error"] = categories.Error;
        }

        return View(new EventDetailsViewModel
        {
            Event = response.Data,
            Categories = categories.Data ?? new()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        if (!IsOrganiser)
        {
            return AccessDenied();
        }

        var response = await api.SendAsync<List<EventViewModel>>(
            HttpMethod.Get, "api/events");

        if (!response.Success)
        {
            TempData["Error"] = response.Error;
        }

        int userId = HttpContext.Session.GetInt32("UserID")!.Value;

        var events = (response.Data ?? new())
            .Where(e => e.OrganiserID == userId)
            .OrderByDescending(e => e.EventDate)
            .ToList();

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsOrganiser)
        {
            return AccessDenied();
        }

        var model = new EventFormViewModel();
        await LoadEventTypes(model);

        return View("Edit", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsOrganiser)
        {
            return AccessDenied();
        }

        var response = await api.SendAsync<EventViewModel>(
            HttpMethod.Get, $"api/events/{id}");

        if (!response.Success || response.Data is null)
        {
            TempData["Error"] = response.Error;
            return RedirectToAction(nameof(Manage));
        }

        var item = response.Data;

        if (item.OrganiserID !=
            HttpContext.Session.GetInt32("UserID"))
        {
            return StatusCode(403, "You can only edit your own events.");
        }

        var model = new EventFormViewModel
        {
            EventID = item.EventID,
            EventName = item.EventName,
            Description = item.Description,
            EventDate = item.EventDate,
            Location = item.Location,
            Distance = item.Distance,
            EventTypeID = item.EventTypeID
        };

        await LoadEventTypes(model);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Save(EventFormViewModel model)
    {
        if (!IsOrganiser)
        {
            return AccessDenied();
        }

        if (!ModelState.IsValid)
        {
            await LoadEventTypes(model);
            return View("Edit", model);
        }

        bool creating = model.EventID == 0;

        var response = await api.SendAsync<JsonElement>(
            creating ? HttpMethod.Post : HttpMethod.Put,
            creating ? "api/events" : $"api/events/{model.EventID}",
            new
            {
                model.EventName,
                model.Description,
                model.EventDate,
                model.Location,
                model.Distance,
                model.EventTypeID
            });

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error);
            await LoadEventTypes(model);
            return View("Edit", model);
        }

        TempData["Success"] = creating
            ? "Event created successfully."
            : "Event updated successfully.";

        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsOrganiser)
        {
            return AccessDenied();
        }

        var response = await api.SendAsync<JsonElement>(
            HttpMethod.Delete, $"api/events/{id}");

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        TempData[response.Success ? "Success" : "Error"] =
            response.Success
                ? "Event deleted."
                : response.Error;

        return RedirectToAction(nameof(Manage));
    }

    private async Task LoadEventTypes(EventFormViewModel model)
    {
        var response = await api.SendAsync<List<EventTypeViewModel>>(
            HttpMethod.Get, "api/eventtypes");

        model.EventTypes = response.Data ?? new();

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error);
        }
    }
}