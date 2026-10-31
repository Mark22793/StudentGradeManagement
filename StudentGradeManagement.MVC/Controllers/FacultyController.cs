using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.MVC.ViewModels;

namespace StudentGradeManagement.MVC.Controllers
{
    public class FacultyController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var token = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token) || role != "Faculty")
                return RedirectToAction("Login", "Account");

            var model = new FacultyDashboardViewModel
            {
                FacultyName = HttpContext.Session.GetString("FullName") ?? "Faculty Member",
                FacultyId = HttpContext.Session.GetString("DisplayNumber")
                            ?? HttpContext.Session.GetString("UserName")
                            ?? "FACULTY"
            };

            return View(model);
        }
    }
}
