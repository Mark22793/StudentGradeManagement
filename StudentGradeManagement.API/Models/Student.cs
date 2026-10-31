using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentGradeManagement.API.Models
{
    [Table("Students", Schema = "dbo")]
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(30)]
        public string StudentNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}