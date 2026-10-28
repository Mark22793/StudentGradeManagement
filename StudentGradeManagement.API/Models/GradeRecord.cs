using System;
using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagement.API.Models
{
    public class GradeRecord
    {
        [Key]
        public int GradeId { get; set; }

        [Required]
        [StringLength(20)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string SubjectCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FacultyId { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
        public int Grade { get; set; }

        [Required]
        [StringLength(20)]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Semester { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string SchoolYear { get; set; } = string.Empty;

        public DateTime DatePosted { get; set; } = DateTime.UtcNow;

        public DateTime? DateModified { get; set; }
    }
}