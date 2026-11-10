using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class EnrolmentsController(RaceDayApiClient api) : Controller
{
    private IActionResult? CheckRole(string role)
    {
        if (!HttpContext.Session.GetInt32("UserID").HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (HttpContext.Session.GetString("Role") != role)
        {
            TempData["Error"] =
                $"This page is available to {role.ToLowerInvariant()} users.";

            return RedirectToAction("Index", "Events");
        }

        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Enter(EnrolmentFormViewModel model)
    {
        var denied = CheckRole("Participant");

        if (denied is not null)
        {
            return denied;
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please select a valid event category.";

            return RedirectToAction(
                "Details", "Events", new { id = model.EventID });
        }

        var response = await api.SendAsync<JsonElement>(
            HttpMethod.Post,
            "api/enrolments",
            new { model.EventID, model.CategoryID });

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.Success)
        {
            TempData["Error"] = response.Error;

            return RedirectToAction(
                "Details", "Events", new { id = model.EventID });
        }

        TempData["Success"] = "You have successfully entered the event.";

        return RedirectToAction(nameof(Mine));
    }

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var denied = CheckRole("Participant");

        if (denied is not null)
        {
            return denied;
        }

        var response = await api.SendAsync<List<EnrolmentViewModel>>(
            HttpMethod.Get, "api/enrolments/me");

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.Success)
        {
            TempData["Error"] = response.Error;
            return RedirectToAction("Index", "Events");
        }

        return View(response.Data ?? new List<EnrolmentViewModel>());
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var denied = CheckRole("Participant");

        if (denied is not null)
        {
            return denied;
        }

        var response = await api.SendAsync<JsonElement>(
            HttpMethod.Delete, $"api/enrolments/{id}");

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        TempData[response.Success ? "Success" : "Error"] =
            response.Success
                ? "Your enrolment was cancelled."
                : response.Error;

        return RedirectToAction(nameof(Mine));
    }

    [HttpGet]
    public async Task<IActionResult> Event(int eventId)
    {
        var denied = CheckRole("Organiser");

        if (denied is not null)
        {
            return denied;
        }

        var raceEvent = await api.SendAsync<EventViewModel>(
            HttpMethod.Get, $"api/events/{eventId}");

        if (!raceEvent.Success || raceEvent.Data is null)
        {
            TempData["Error"] = raceEvent.Error;
            return RedirectToAction("Manage", "Events");
        }

        var response = await api.SendAsync<List<EnrolmentViewModel>>(
            HttpMethod.Get, $"api/events/{eventId}/enrolments");

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.Success)
        {
            TempData["Error"] = response.Error;
            return RedirectToAction("Manage", "Events");
        }

        return View(new EventEnrolmentsViewModel
        {
            Event = raceEvent.Data,
            Enrolments = response.Data ?? new()
        });
    }
}