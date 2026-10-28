using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.MVC.ViewModels;

namespace StudentGradeManagement.MVC.Controllers
{
    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Student")
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new StudentDashboardViewModel
            {
                StudentName = HttpContext.Session.GetString("FullName") ?? "Student",
                StudentId = HttpContext.Session.GetString("UserName") ?? "STUDENT"
            };

            return View(model);
        }
    }
}