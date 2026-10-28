using Microsoft.AspNetCore.Identity;
using StudentGradeManagement.API.Models;

namespace StudentGradeManagement.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Create Roles if not exist
            string[] roles = new[] { "Faculty", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Default Faculty User
            var facultyUser = await userManager.FindByNameAsync("prof_smith");
            if (facultyUser == null)
            {
                var faculty = new ApplicationUser
                {
                    UserName = "prof_smith",
                    Email = "prof_smith@school.edu",
                    FullName = "Prof. John Smith",
                    FacultyId = "FAC-2026-001",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(faculty, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(faculty, "Faculty");
                }
            }

            // Seed Default Student User
            var studentUser = await userManager.FindByNameAsync("202310123");
            if (studentUser == null)
            {
                var student = new ApplicationUser
                {
                    UserName = "202310123",
                    Email = "student1@school.edu",
                    FullName = "Juan Dela Cruz",
                    StudentId = "202310123",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(student, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(student, "Student");
                }
            }
        }
    }
}