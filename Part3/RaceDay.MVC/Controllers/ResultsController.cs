using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class ResultsController : Controller
{
    private readonly RaceDayApiClient _api;

    public ResultsController(RaceDayApiClient api)
    {
        _api = api;
    }

    private IActionResult? CheckRole(string requiredRole)
    {
        if (!HttpContext.Session.GetInt32("UserID").HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (HttpContext.Session.GetString("Role") != requiredRole)
        {
            TempData["Error"] =
                "You do not have permission to access this page.";

            return RedirectToAction("Index", "Events");
        }

        return null;
    }

    // Participants view their own results.
    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var access = CheckRole("Participant");

        if (access is not null)
        {
            return access;
        }

        var response = await _api.SendAsync<List<ResultViewModel>>(
            HttpMethod.Get,
            "api/results/me");

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.Success)
        {
            TempData["Error"] = response.Error;
            return RedirectToAction("Index", "Events");
        }

        return View(response.Data ?? new List<ResultViewModel>());
    }

    // Organisers view and capture results for their event.
    [HttpGet]
    public async Task<IActionResult> Manage(int eventId)
    {
        var access = CheckRole("Organiser");

        if (access is not null)
        {
            return access;
        }

        var page = await LoadPage(eventId);

        if (page is null)
        {
            return RedirectAfterLoadFailure();
        }

        return View(page);
    }

    // Load an existing result into the form.
    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var access = CheckRole("Organiser");

        if (access is not null)
        {
            return access;
        }

        var page = await LoadPage(eventId);

        if (page is null)
        {
            return RedirectAfterLoadFailure();
        }

        var result = page.Results.SingleOrDefault(
            item => item.ResultID == id);

        if (result is null)
        {
            TempData["Error"] = "Result not found for this event.";

            return RedirectToAction(nameof(Manage), new { eventId });
        }

        page.Form = new ResultFormViewModel
        {
            ResultID = result.ResultID,
            EventID = eventId,
            EnrolmentID = result.EnrolmentID,
            FinishTime = result.FinishTime?.ToString(@"hh\:mm\:ss")
                ?? string.Empty,
            FinishingPosition = result.FinishingPosition
        };

        return View("Manage", page);
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [Bind(Prefix = "Form")] ResultFormViewModel form)
    {
        var access = CheckRole("Organiser");

        if (access is not null)
        {
            return access;
        }

        var page = await LoadPage(form.EventID);

        if (page is null)
        {
            return RedirectAfterLoadFailure();
        }

        page.Form = form;

        var entry = page.Enrolments.SingleOrDefault(
            item => item.EnrolmentID == form.EnrolmentID);

        if (entry is null)
        {
            ModelState.AddModelError(
                "Form.EnrolmentID",
                "Select a participant entry belonging to this event.");
        }

        if (form.ResultID != 0)
        {
            var existingResult = page.Results.SingleOrDefault(
                item => item.ResultID == form.ResultID);

            if (existingResult is null
                || existingResult.EnrolmentID != form.EnrolmentID)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The selected result does not match this event entry.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Manage", page);
        }

        var body = new
        {
            form.FinishTime,
            form.FinishingPosition
        };

        var response = form.ResultID == 0
            ? await _api.SendAsync<ResultViewModel>(
                HttpMethod.Post,
                $"api/enrolments/{form.EnrolmentID}/result",
                body)
            : await _api.SendAsync<ResultViewModel>(
                HttpMethod.Put,
                $"api/results/{form.ResultID}",
                body);

        if (response.StatusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error);
            return View("Manage", page);
        }

        TempData["Success"] = form.ResultID == 0
            ? "Result recorded successfully."
            : "Result updated successfully.";

        return RedirectToAction(
            nameof(Manage),
            new { eventId = form.EventID });
    }

    private async Task<EventResultsViewModel?> LoadPage(int eventId)
    {
        var eventResponse = await _api.SendAsync<EventViewModel>(
            HttpMethod.Get,
            $"api/events/{eventId}");

        if (!eventResponse.Success || eventResponse.Data is null)
        {
            TempData["Error"] = eventResponse.Error;
            return null;
        }

        if (eventResponse.Data.OrganiserID
            != HttpContext.Session.GetInt32("UserID"))
        {
            TempData["Error"] =
                "You can only manage results for your own events.";

            return null;
        }

        var enrolmentsResponse =
            await _api.SendAsync<List<EnrolmentViewModel>>(
                HttpMethod.Get,
                $"api/events/{eventId}/enrolments");

        if (!enrolmentsResponse.Success)
        {
            TempData["Error"] = enrolmentsResponse.Error;
            return null;
        }

        var resultsResponse =
            await _api.SendAsync<List<ResultViewModel>>(
                HttpMethod.Get,
                $"api/events/{eventId}/results");

        if (!resultsResponse.Success)
        {
            TempData["Error"] = resultsResponse.Error;
            return null;
        }

        return new EventResultsViewModel
        {
            Event = eventResponse.Data,
            Enrolments = enrolmentsResponse.Data
                ?? new List<EnrolmentViewModel>(),
            Results = resultsResponse.Data
                ?? new List<ResultViewModel>(),
            Form = new ResultFormViewModel
            {
                EventID = eventId
            }
        };
    }

    private IActionResult RedirectAfterLoadFailure()
    {
        if (!HttpContext.Session.GetInt32("UserID").HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        return RedirectToAction("Manage", "Events");
    }
}