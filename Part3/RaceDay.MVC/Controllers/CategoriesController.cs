using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class CategoriesController(RaceDayApiClient api) : Controller
{
    private IActionResult? CheckAccess()
    {
        if (!HttpContext.Session.GetInt32("UserID").HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (HttpContext.Session.GetString("Role") != "Organiser")
        {
            TempData["Error"] = "Only organisers can manage categories.";
            return RedirectToAction("Index", "Events");
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> Manage(int eventId)
    {
        var denied = CheckAccess();

        if (denied is not null)
        {
            return denied;
        }

        var result = await LoadPage(eventId);

        if (result.Page is null)
        {
            return result.Error!;
        }

        return View(result.Page);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var denied = CheckAccess();

        if (denied is not null)
        {
            return denied;
        }

        var result = await LoadPage(eventId);

        if (result.Page is null)
        {
            return result.Error!;
        }

        var category = result.Page.Categories
            .FirstOrDefault(c => c.CategoryID == id);

        if (category is null)
        {
            TempData["Error"] = "Category not found.";
            return RedirectToAction(nameof(Manage), new { eventId });
        }

        result.Page.Form = new CategoryFormViewModel
        {
            CategoryID = category.CategoryID,
            EventID = eventId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            MaximumParticipants = category.MaximumParticipants
        };

        return View("Manage", result.Page);
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [Bind(Prefix = "Form")] CategoryFormViewModel model)
    {
        var denied = CheckAccess();

        if (denied is not null)
        {
            return denied;
        }

        var page = await LoadPage(model.EventID);

        if (page.Page is null)
        {
            return page.Error!;
        }

        if (model.CategoryID != 0
            && !page.Page.Categories.Any(c =>
                c.CategoryID == model.CategoryID))
        {
            TempData["Error"] = "Category not found for this event.";

            return RedirectToAction(
                nameof(Manage), new { eventId = model.EventID });
        }

        if (!ModelState.IsValid)
        {
            page.Page.Form = model;
            return View("Manage", page.Page);
        }

        bool creating = model.CategoryID == 0;

        var response = await api.SendAsync<JsonElement>(
            creating ? HttpMethod.Post : HttpMethod.Put,
            creating
                ? $"api/events/{model.EventID}/categories"
                : $"api/categories/{model.CategoryID}",
            new
            {
                model.CategoryName,
                model.Description,
                model.MaximumParticipants
            });

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error);
            page.Page.Form = model;
            return View("Manage", page.Page);
        }

        TempData["Success"] = creating
            ? "Category created."
            : "Category updated.";

        return RedirectToAction(
            nameof(Manage), new { eventId = model.EventID });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var denied = CheckAccess();

        if (denied is not null)
        {
            return denied;
        }

        var page = await LoadPage(eventId);

        if (page.Page is null)
        {
            return page.Error!;
        }

        if (!page.Page.Categories.Any(c => c.CategoryID == id))
        {
            TempData["Error"] = "Category not found for this event.";
            return RedirectToAction(nameof(Manage), new { eventId });
        }

        var response = await api.SendAsync<JsonElement>(
            HttpMethod.Delete, $"api/categories/{id}");

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        TempData[response.Success ? "Success" : "Error"] =
            response.Success ? "Category deleted." : response.Error;

        return RedirectToAction(nameof(Manage), new { eventId });
    }

    private async Task<(
        CategoryManageViewModel? Page,
        IActionResult? Error)> LoadPage(int eventId)
    {
        var raceEvent = await api.SendAsync<EventViewModel>(
            HttpMethod.Get, $"api/events/{eventId}");

        if (!raceEvent.Success || raceEvent.Data is null)
        {
            TempData["Error"] = raceEvent.Error;

            return (null, RedirectToAction("Manage", "Events"));
        }

        if (raceEvent.Data.OrganiserID !=
            HttpContext.Session.GetInt32("UserID"))
        {
            TempData["Error"] =
                "You can only manage categories for your own events.";

            return (null, RedirectToAction("Manage", "Events"));
        }

        var categories = await api.SendAsync<List<CategoryViewModel>>(
            HttpMethod.Get, $"api/events/{eventId}/categories");

        if (!categories.Success)
        {
            TempData["Error"] = categories.Error;

            return (null, RedirectToAction("Manage", "Events"));
        }

        return (new CategoryManageViewModel
        {
            Event = raceEvent.Data,
            Categories = categories.Data ?? new(),
            Form = new CategoryFormViewModel { EventID = eventId }
        }, null);
    }
}