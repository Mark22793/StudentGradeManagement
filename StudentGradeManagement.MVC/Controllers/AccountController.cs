using Microsoft.AspNetCore.Mvc;
using StudentGradeManagement.MVC.ViewModels;
using System.Text;
using System.Text.Json;

namespace StudentGradeManagement.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private string ApiBaseUrl =>
            _configuration["ApiBaseUrl"] ?? "https://localhost:7001";

        [HttpGet]
        public IActionResult Login()
        {
            // Already signed in? Send them to their dashboard.
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(role))
                return RedirectToDashboard(role);

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new { username = model.Username, password = model.Password };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{ApiBaseUrl}/api/auth/login", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View(model);
                }

                var responseString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                string token = GetJsonProperty(root, "token");
                string role = GetJsonProperty(root, "role");
                string username = GetJsonProperty(root, "username");
                string fullName = GetJsonProperty(root, "fullName");
                string studentNumber = GetJsonProperty(root, "studentNumber");
                string facultyNumber = GetJsonProperty(root, "facultyNumber");

                if (string.IsNullOrEmpty(username)) username = model.Username;
                if (string.IsNullOrEmpty(fullName)) fullName = username;

                if (string.IsNullOrEmpty(token))
                {
                    ModelState.AddModelError("", "Invalid authentication response from server.");
                    return View(model);
                }

                var displayNumber = role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)
                    ? (string.IsNullOrEmpty(facultyNumber) ? username : facultyNumber)
                    : (string.IsNullOrEmpty(studentNumber) ? username : studentNumber);

                HttpContext.Session.SetString("JWToken", token);
                HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("UserName", username);
                HttpContext.Session.SetString("FullName", fullName);
                HttpContext.Session.SetString("DisplayNumber", displayNumber);

                return RedirectToDashboard(role);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Connection error: {ex.Message}. Is the API running on {ApiBaseUrl}?");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        private IActionResult RedirectToDashboard(string role) =>
            role.Equals("Faculty", StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction("Index", "Faculty")
                : RedirectToAction("Index", "Student");

        private static string GetJsonProperty(JsonElement element, string propertyName)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.ValueKind == JsonValueKind.Null ? string.Empty : prop.Value.GetString() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
