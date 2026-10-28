using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.API.Models;

namespace StudentGradeManagement.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<GradeRecord> GradeRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure GradeRecord Table Constraints
            builder.Entity<GradeRecord>(entity =>
            {
                entity.ToTable("GradeRecords");
                entity.HasKey(g => g.GradeId);

                entity.Property(g => g.StudentId).IsRequired().HasMaxLength(20);
                entity.Property(g => g.SubjectCode).IsRequired().HasMaxLength(20);
                entity.Property(g => g.SubjectName).IsRequired().HasMaxLength(100);
                entity.Property(g => g.FacultyId).IsRequired().HasMaxLength(20);
                entity.Property(g => g.Remarks).IsRequired().HasMaxLength(20);
                entity.Property(g => g.Semester).IsRequired().HasMaxLength(20);
                entity.Property(g => g.SchoolYear).IsRequired().HasMaxLength(20);

                // Composite index to enforce unique grade entry per student, subject, semester, and SY
                entity.HasIndex(g => new { g.StudentId, g.SubjectCode, g.Semester, g.SchoolYear }).IsUnique();
            });
        }
    }
}