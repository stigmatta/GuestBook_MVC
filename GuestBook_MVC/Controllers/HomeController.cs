using Microsoft.AspNetCore.Mvc;
using GuestBook_MVC.Models;
using GuestBook_MVC.Repositories;
using Sodium;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace GuestBook_MVC.Controllers;

public class HomeController : Controller
{
    private readonly IRepository _repo;

    public HomeController(IRepository repo)
    {
        _repo = repo;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetMessages()
    {
        var messages = await _repo.GetMessages();
        var result = messages.Select(m => new
        {
            user = m.User?.Name ?? "Unknown",
            text = m.MessageText,
            date = m.SendDate.ToString("g")
        });
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] MessageViewModel model)
    {
        var username = HttpContext.Session.GetString("username");
        var user = !string.IsNullOrEmpty(username)
            ? await _repo.GetUser(username)
            : null;

        if (user == null || string.IsNullOrWhiteSpace(model.MessageText))
            return Json(new { success = false, error = "Unauthorized or empty message." });

        var message = new Message
        {
            User = user,
            MessageText = model.MessageText,
            SendDate = DateTime.Now
        };

        await _repo.AddMessage(message);
        await _repo.Save();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AjaxLogin([FromBody] User user)
    {
        var existingUser = await _repo.GetUser(user.Name);
        if (existingUser == null || !PasswordHash.ArgonHashStringVerify(existingUser.Password, user.Password))
            return Json(new { success = false, error = "Invalid username or password" });

        HttpContext.Session.SetString("username", existingUser.Name);
        return Json(new { success = true, isGuest = false });
    }

    [HttpPost]
    public async Task<IActionResult> Registration([FromBody] User user)
    {
        if (user.Password != user.ConfirmPassword)
            return Json(new { success = false, error = "Passwords do not match" });

        user.Password = PasswordHash.ArgonHashString(user.Password, PasswordHash.StrengthArgon.Interactive);

        await _repo.AddUser(user);
        try
        {
            await _repo.Save();
        }
        catch (DbUpdateException)
        {
            return Json(new { success = false, error = "Username already taken" });
        }

        HttpContext.Session.SetString("username", user.Name);
        return Json(new { success = true, isGuest = false });
    }

    [HttpPost]
    public IActionResult GuestLogin()
    {
        return Json(new { success = true, isGuest = true });
    }

    [HttpPost]
    public IActionResult LogOut()
    {
        HttpContext.Session.Clear();
        return Json(new { success = true });
    }
}
