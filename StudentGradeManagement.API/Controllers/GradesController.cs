using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeManagement.API.Data;
using StudentGradeManagement.API.DTOs;
using StudentGradeManagement.API.Helpers;
using StudentGradeManagement.API.Models;
using System.Security.Claims;

namespace StudentGradeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GradesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GradesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/grades (Faculty Only)
        [HttpGet]
        [Authorize(Roles = "Faculty")]
        public async Task<ActionResult<IEnumerable<GradeResponseDto>>> GetAllGrades()
        {
            var grades = await _context.GradeRecords
                .Select(g => MapToDto(g))
                .ToListAsync();

            return Ok(grades);
        }

        // GET: api/grades/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GradeResponseDto>> GetGradeById(int id)
        {
            var grade = await _context.GradeRecords.FindAsync(id);
            if (grade == null)
                return NotFound(new { message = "Grade record not found." });

            // Data Ownership Enforcement for Students
            if (User.IsInRole("Student"))
            {
                var studentIdClaim = User.FindFirstValue("StudentId");
                if (string.IsNullOrEmpty(studentIdClaim) || grade.StudentId != studentIdClaim)
                    return NotFound(new { message = "Grade record not found." });
            }

            return Ok(MapToDto(grade));
        }

        // GET: api/student/grades (Student Only)
        [HttpGet("/api/student/grades")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<IEnumerable<GradeResponseDto>>> GetMyGrades()
        {
            var studentIdClaim = User.FindFirstValue("StudentId");
            if (string.IsNullOrEmpty(studentIdClaim))
                return Unauthorized(new { message = "Invalid student identity." });

            var myGrades = await _context.GradeRecords
                .Where(g => g.StudentId == studentIdClaim)
                .Select(g => MapToDto(g))
                .ToListAsync();

            return Ok(myGrades);
        }

        // POST: api/grades (Faculty Only)
        [HttpPost]
        [Authorize(Roles = "Faculty")]
        public async Task<ActionResult<GradeResponseDto>> CreateGrade([FromBody] SaveGradeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Grade < 0 || dto.Grade > 100)
                return BadRequest(new { message = "Grade must be between 0 and 100." });

            bool exists = await _context.GradeRecords.AnyAsync(g =>
                g.StudentId == dto.StudentId &&
                g.SubjectCode == dto.SubjectCode &&
                g.Semester == dto.Semester &&
                g.SchoolYear == dto.SchoolYear);

            if (exists)
                return BadRequest(new { message = "A grade record already exists for this subject/semester." });

            var facultyId = User.FindFirstValue("FacultyId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newGrade = new GradeRecord
            {
                StudentId = dto.StudentId,
                SubjectCode = dto.SubjectCode,
                SubjectName = dto.SubjectName,
                FacultyId = facultyId ?? "FACULTY",
                Grade = dto.Grade,
                Remarks = GradeCalculator.ComputeRemarks(dto.Grade), // Server-side calculation
                Semester = dto.Semester,
                SchoolYear = dto.SchoolYear,
                DatePosted = DateTime.UtcNow
            };

            _context.GradeRecords.Add(newGrade);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGradeById), new { id = newGrade.GradeId }, MapToDto(newGrade));
        }

        // PUT: api/grades/{id} (Faculty Only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UpdateGrade(int id, [FromBody] SaveGradeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var gradeRecord = await _context.GradeRecords.FindAsync(id);
            if (gradeRecord == null)
                return NotFound(new { message = "Grade record not found." });

            if (dto.Grade < 0 || dto.Grade > 100)
                return BadRequest(new { message = "Grade must be between 0 and 100." });

            gradeRecord.StudentId = dto.StudentId;
            gradeRecord.SubjectCode = dto.SubjectCode;
            gradeRecord.SubjectName = dto.SubjectName;
            gradeRecord.Grade = dto.Grade;
            gradeRecord.Remarks = GradeCalculator.ComputeRemarks(dto.Grade); // Re-calculate remark
            gradeRecord.Semester = dto.Semester;
            gradeRecord.SchoolYear = dto.SchoolYear;
            gradeRecord.DateModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(gradeRecord));
        }

        // DELETE: api/grades/{id} (Faculty Only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var gradeRecord = await _context.GradeRecords.FindAsync(id);
            if (gradeRecord == null)
                return NotFound(new { message = "Grade record not found." });

            _context.GradeRecords.Remove(gradeRecord);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static GradeResponseDto MapToDto(GradeRecord g) => new GradeResponseDto
        {
            GradeId = g.GradeId,
            StudentId = g.StudentId,
            SubjectCode = g.SubjectCode,
            SubjectName = g.SubjectName,
            FacultyId = g.FacultyId,
            Grade = g.Grade,
            Remarks = g.Remarks,
            Semester = g.Semester,
            SchoolYear = g.SchoolYear,
            DatePosted = g.DatePosted,
            DateModified = g.DateModified
        };
    }
}