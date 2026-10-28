using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudentGradeManagement.API.DTOs;
using StudentGradeManagement.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudentGradeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid username or password."
                });
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var role = userRoles.FirstOrDefault() ?? "Student";

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, role)
            };

            // Attach StudentId or FacultyId claim depending on mapping
            if (!string.IsNullOrEmpty(user.StudentId))
                authClaims.Add(new Claim("StudentId", user.StudentId));

            if (!string.IsNullOrEmpty(user.FacultyId))
                authClaims.Add(new Claim("FacultyId", user.FacultyId));

            var jwtSecret = _configuration["Jwt:Secret"] ?? "SuperSecretKeyForStudentGradeManagementSystem2026!";
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "GradeManagementAPI",
                audience: _configuration["Jwt:Audience"] ?? "GradeManagementApp",
                expires: DateTime.UtcNow.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Login successful.",
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Role = role,
                FullName = user.FullName
            });
        }
    }
}