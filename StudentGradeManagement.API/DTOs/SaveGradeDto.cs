using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagement.API.DTOs
{
    public class SaveGradeDto
    {
        [Required(ErrorMessage = "Student ID is required.")]
        public string StudentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject code is required.")]
        [StringLength(20)]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject name is required.")]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade is required.")]
        [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
        public int Grade { get; set; }

        [Required(ErrorMessage = "Semester is required.")]
        [StringLength(20)]
        public string Semester { get; set; } = string.Empty;

        [Required(ErrorMessage = "School year is required.")]
        [StringLength(20)]
        public string SchoolYear { get; set; } = string.Empty;
    }
}