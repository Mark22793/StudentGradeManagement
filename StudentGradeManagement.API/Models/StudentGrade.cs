using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentGradeManagement.API.Models
{
    [Table("StudentGrades", Schema = "dbo")]
    public class StudentGrade
    {
        [Key]
        public int GradeId { get; set; }
        public int StudentId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int FacultyId { get; set; }
        public int Grade { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string SchoolYear { get; set; } = string.Empty;
        public DateTime DatePosted { get; set; } = DateTime.UtcNow;
        public DateTime? DateModified { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("FacultyId")]
        public virtual Faculty? Faculty { get; set; }
    }
}