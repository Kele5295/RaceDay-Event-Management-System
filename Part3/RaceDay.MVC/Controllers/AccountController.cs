using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class AccountController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await api.SendAsync<LoginResponse>(
            HttpMethod.Post,
            "api/auth/login",
            new { model.Email, model.Password });

        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(
                string.Empty,
                response.Error.Length > 0
                    ? response.Error
                    : "Login could not be completed.");

            return View(model);
        }

        if (HttpContext.Session.GetString("ApiSessionCookie") is null)
        {
            HttpContext.Session.Clear();

            ModelState.AddModelError(
                string.Empty,
                "The API did not create a login session. Please try again.");

            return View(model);
        }

        HttpContext.Session.SetInt32("UserID", response.Data.UserID);
        HttpContext.Session.SetString("Role", response.Data.Role);
        HttpContext.Session.SetString(
            "DisplayName",
            $"{response.Data.FirstName} {response.Data.LastName}");

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await api.SendAsync<JsonElement>(
            HttpMethod.Post,
            "api/auth/register",
            new
            {
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.Role,
                model.PhoneNumber
            });

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error);
            return View(model);
        }

        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await api.SendAsync<JsonElement>(
            HttpMethod.Post, "api/auth/logout");

        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!HttpContext.Session.GetInt32("UserID").HasValue)
        {
            return RedirectToAction(nameof(Login));
        }

        var response = await api.SendAsync<ProfileViewModel>(
            HttpMethod.Get, "api/profile");

        if (response.StatusCode == 401)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!response.Success || response.Data is null)
        {
            TempData["Error"] = response.Error;
            return RedirectToAction("Index", "Home");
        }

        return View(response.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!HttpContext.Session.GetInt32("UserID").HasValue)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await api.SendAsync<JsonElement>(
            HttpMethod.Put,
            "api/profile",
            new
            {
                model.FirstName,
                model.LastName,
                model.PhoneNumber
            });

        if (response.StatusCode == 401)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error);
            return View(model);
        }

        HttpContext.Session.SetString(
            "DisplayName", $"{model.FirstName} {model.LastName}");

        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Profile));
    }
}