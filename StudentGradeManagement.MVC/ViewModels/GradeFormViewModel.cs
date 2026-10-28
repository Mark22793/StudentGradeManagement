using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagement.MVC.ViewModels
{
    public class GradeFormViewModel
    {
        public int? GradeId { get; set; }

        [Required(ErrorMessage = "Student ID is required.")]
        [Display(Name = "Student Number / ID")]
        public string StudentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject Code is required.")]
        [Display(Name = "Subject Code")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject Name is required.")]
        [Display(Name = "Subject Name")]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade is required.")]
        [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
        [Display(Name = "Numerical Grade")]
        public int Grade { get; set; }

        [Required(ErrorMessage = "Semester is required.")]
        public string Semester { get; set; } = string.Empty;

        [Required(ErrorMessage = "School Year is required.")]
        [Display(Name = "School Year")]
        public string SchoolYear { get; set; } = string.Empty;
    }
}