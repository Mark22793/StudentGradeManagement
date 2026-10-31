using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.API.Helpers;
using StudentGradeManagement.API.Models;

namespace StudentGradeManagement.API.Data
{
    /// <summary>
    /// Idempotent seeder. Safe to run on every startup:
    ///  - creates roles,
    ///  - ensures the default Identity (login) accounts exist,
    ///  - ensures a matching domain row (Students / Faculty) exists for EVERY
    ///    Identity user that has a role (so logins map to real records),
    ///  - seeds a few sample grades only when none exist.
    /// Existing rows are never overwritten or deleted.
    /// </summary>
    public static class DbSeeder
    {
        private const string DefaultPassword = "Password123!";

        // (username, password, fullName, email, number)
        private static readonly (string User, string Pass, string Name, string Email, string Number)[] FacultyAccounts =
        {
            ("prof_smith", DefaultPassword, "Andrea Garcia",  "andrea.garcia@example.com",  "FAC-001"),
            ("FAC-002",    DefaultPassword, "Michael Ramos",  "michael.ramos@example.com",  "FAC-002"),
        };

        private static readonly (string User, string Pass, string Name, string Email, string Number)[] StudentAccounts =
        {
            ("202310123", DefaultPassword, "Juan Dela Cruz", "student1@school.edu",      "202310123"),
            ("2026-0001", DefaultPassword, "Juan Dela Cruz", "juan.delacruz@example.com","2026-0001"),
            ("2026-0002", DefaultPassword, "Maria Santos",   "maria.santos@example.com", "2026-0002"),
            ("2026-0003", DefaultPassword, "Pedro Reyes",    "pedro.reyes@example.com",  "2026-0003"),
        };

        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 1. Roles
            foreach (var role in new[] { "Faculty", "Student" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2. Default Identity (login) accounts
            foreach (var f in FacultyAccounts)
                await EnsureUserAsync(userManager, f.User, f.Pass, f.Name, f.Email, "Faculty", facultyNumber: f.Number);

            foreach (var s in StudentAccounts)
                await EnsureUserAsync(userManager, s.User, s.Pass, s.Name, s.Email, "Student", studentNumber: s.Number);

            // 3. Reconcile domain rows for EVERY identity user that has a role.
            await EnsureDomainRowsAsync(context, userManager);

            // 4. Sample grades (only if the table is empty)
            await SeedSampleGradesAsync(context);
        }

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string userName, string password, string fullName, string email, string role,
            string? studentNumber = null, string? facultyNumber = null)
        {
            var existing = await userManager.FindByNameAsync(userName);
            if (existing != null)
            {
                // Make sure role membership is present; never change the password.
                if (!await userManager.IsInRoleAsync(existing, role))
                    await userManager.AddToRoleAsync(existing, role);
                return;
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                StudentId = studentNumber,
                FacultyId = facultyNumber
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }

        private static async Task EnsureDomainRowsAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            // Faculty
            var facultyUsers = await userManager.GetUsersInRoleAsync("Faculty");
            foreach (var u in facultyUsers)
            {
                var number = !string.IsNullOrWhiteSpace(u.FacultyId) ? u.FacultyId! : u.UserName!;
                var (first, last) = SplitName(u.FullName);

                bool exists = await context.Faculty.AnyAsync(f => f.FacultyNumber == number || f.Email == u.Email);
                if (!exists)
                {
                    context.Faculty.Add(new Faculty
                    {
                        FacultyNumber = number,
                        FirstName = first,
                        LastName = last,
                        Email = u.Email ?? $"{number}@school.edu",
                        IsActive = true
                    });
                }
            }

            // Students
            var studentUsers = await userManager.GetUsersInRoleAsync("Student");
            foreach (var u in studentUsers)
            {
                var number = !string.IsNullOrWhiteSpace(u.StudentId) ? u.StudentId! : u.UserName!;
                var (first, last) = SplitName(u.FullName);

                bool exists = await context.Students.AnyAsync(s => s.StudentNumber == number || s.Email == u.Email);
                if (!exists)
                {
                    context.Students.Add(new Student
                    {
                        StudentNumber = number,
                        FirstName = first,
                        LastName = last,
                        Email = u.Email ?? $"{number}@school.edu",
                        IsActive = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedSampleGradesAsync(ApplicationDbContext context)
        {
            if (await context.StudentGrades.AnyAsync())
                return; // never overwrite existing grade data

            var faculty = await context.Faculty.OrderBy(f => f.FacultyId).FirstOrDefaultAsync();
            if (faculty == null) return;

            var students = await context.Students.ToDictionaryAsync(s => s.StudentNumber, s => s.StudentId);

            // (studentNumber, subjectCode, subjectName, grade, semester, schoolYear)
            var samples = new (string Num, string Code, string Name, int Grade, string Sem, string Sy)[]
            {
                ("2026-0001", "IPT101", "Integrative Programming Technologies", 96, "1st Semester", "2026-2027"),
                ("2026-0001", "ASIA201","Advanced System Integration and Architecture", 88, "1st Semester", "2026-2027"),
                ("2026-0002", "IPT101", "Integrative Programming Technologies", 91, "1st Semester", "2026-2027"),
                ("2026-0002", "WEB101", "Web Systems and Technologies", 73, "1st Semester", "2026-2027"),
                ("2026-0003", "IPT101", "Integrative Programming Technologies", 82, "1st Semester", "2026-2027"),
                ("202310123","ASIA201","Advanced System Integration and Architecture", 79, "1st Semester", "2026-2027"),
            };

            foreach (var s in samples)
            {
                if (!students.TryGetValue(s.Num, out var studentId))
                    continue;

                context.StudentGrades.Add(new StudentGrade
                {
                    StudentId = studentId,
                    SubjectCode = s.Code,
                    SubjectName = s.Name,
                    FacultyId = faculty.FacultyId,
                    Grade = s.Grade,
                    Remarks = GradeCalculator.ComputeRemarks(s.Grade),
                    Semester = s.Sem,
                    SchoolYear = s.Sy,
                    DatePosted = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        private static (string First, string Last) SplitName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ("Unknown", "User");

            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (parts[0], string.Empty);

            return (parts[0], string.Join(' ', parts[1..]));
        }
    }
}
