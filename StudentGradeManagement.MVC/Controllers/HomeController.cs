using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.MVC.Models;

namespace StudentGradeManagement.MVC.Controllers;

public class HomeController : Controller
{
    // Route visitors to their dashboard if signed in, otherwise to the login page.
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Faculty");
        if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Student");

        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
