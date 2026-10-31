using System.ComponentModel.DataAnnotations;

namespace StudentGradeManagement.API.DTOs
{
    /// <summary>
    /// Payload used to create or update a grade record.
    /// NOTE: Grade is intentionally a nullable decimal so the API can
    /// explicitly reject missing, null, decimal, and out-of-range values
    /// with clear messages. Remarks are NEVER accepted from the client;
    /// they are always computed server-side.
    /// </summary>
    public class SaveGradeDto
    {
        [Required(ErrorMessage = "Student number is required.")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject code is required.")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject name is required.")]
        public string SubjectName { get; set; } = string.Empty;

        /// <summary>
        /// Numerical grade. Must be a whole number between 0 and 100.
        /// Decimal, null, and missing values are rejected by the controller.
        /// </summary>
        [Required(ErrorMessage = "Grade is required.")]
        [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
        public decimal? Grade { get; set; }

        [Required(ErrorMessage = "Semester is required.")]
        public string Semester { get; set; } = string.Empty;

        [Required(ErrorMessage = "School year is required.")]
        public string SchoolYear { get; set; } = string.Empty;
    }
}
