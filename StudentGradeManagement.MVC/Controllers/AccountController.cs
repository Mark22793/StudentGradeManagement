using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.MVC.ViewModels;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudentGradeManagement.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("https://localhost:7001/api/auth/login", new
                {
                    username = model.Username,
                    password = model.Password
                });

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage = "Invalid username or password.";
                    return View(model);
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                string token = root.GetProperty("token").GetString() ?? "";
                string role = root.GetProperty("role").GetString() ?? "";
                string fullName = root.GetProperty("fullName").GetString() ?? "";

                HttpContext.Session.SetString("JWToken", token);
                HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("FullName", fullName);
                HttpContext.Session.SetString("UserName", model.Username);

                if (role == "Faculty")
                    return RedirectToAction("Index", "Faculty");

                return RedirectToAction("Index", "Student");
            }
            catch (HttpRequestException)
            {
                model.ErrorMessage = "Cannot connect to the Web API service. Please ensure the API is running on https://localhost:7001.";
                return View(model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}