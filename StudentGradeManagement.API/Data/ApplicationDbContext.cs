using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.API.Models;

namespace StudentGradeManagement.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Faculty> Faculty { get; set; }
        public DbSet<StudentGrade> StudentGrades { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Student>(entity =>
            {
                entity.ToTable("Students", "dbo");
                entity.HasKey(e => e.StudentId);
                entity.HasIndex(e => e.StudentNumber).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            builder.Entity<Faculty>(entity =>
            {
                entity.ToTable("Faculty", "dbo");
                entity.HasKey(e => e.FacultyId);
                entity.HasIndex(e => e.FacultyNumber).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            builder.Entity<StudentGrade>(entity =>
            {
                entity.ToTable("StudentGrades", "dbo");
                entity.HasKey(e => e.GradeId);
                entity.HasIndex(e => new { e.StudentId, e.SubjectCode, e.Semester, e.SchoolYear }).IsUnique();
            });
        }
    }
}