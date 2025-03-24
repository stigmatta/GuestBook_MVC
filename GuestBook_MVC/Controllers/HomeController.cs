using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GuestBook_MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace GuestBook_MVC.Controllers;

public class HomeController : Controller
{
    private readonly MessageContext _context;
    public HomeController(MessageContext messageContext)
    {
        _context = messageContext;
    }

    public async Task<IActionResult> Index()
    {
        var messages = _context.Messages.Include(m => m.User).ToList();

        var viewModel = new MessageViewModel
        {
            Messages = messages
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(MessageViewModel model)
    {
        var user = _context.Users.FirstOrDefault(u => u.Name == HttpContext.Session.GetString("username"));
        if (user != null && !string.IsNullOrEmpty(model.MessageText))
        {
            var _message = new Message
            {
                User = user,
                MessageText = model.MessageText,
                SendDate = DateTime.Now
            };

            _context.Messages.Add(_message);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    public IActionResult LogOut()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }



    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("username") != null)
            return RedirectToAction("Index");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login([Bind("Id","Name","Password")] User user)
    {
        var existingUser = _context.Users.FirstOrDefault(u => user.Name == u.Name);

        if(existingUser == null || existingUser.Password != user.Password)
        {
            ModelState.AddModelError("Password", "Invalid username or password");
            return View(user);
        }
        HttpContext.Session.SetString("username", user.Name);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Registration()
    {
        if (HttpContext.Session.GetString("username") != null)
            return RedirectToAction("Index");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Registration(User user)
    {
        if(user.Password != user.ConfirmPassword)
        {
            ModelState.AddModelError("Password", "Passwords have to be similar");
            return View(user);
        }

        _context.Users.Add(user);
        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("Name", "This username is already taken");
            return View(user);
        }

        return RedirectToAction("Login", "Home");
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
