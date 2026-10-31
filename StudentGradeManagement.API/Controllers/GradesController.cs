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
    [Authorize] // every endpoint requires a valid token (401 otherwise)
    public class GradesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GradesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------------
        // GET: api/grades   (FACULTY ONLY) - view all records, with optional
        // server-side filters: studentNumber, subject, semester, schoolYear, remarks.
        // ------------------------------------------------------------------
        [HttpGet]
        [Authorize(Roles = "Faculty")]
        public async Task<ActionResult<IEnumerable<GradeResponseDto>>> GetAllGrades(
            [FromQuery] string? studentNumber,
            [FromQuery] string? subject,
            [FromQuery] string? semester,
            [FromQuery] string? schoolYear,
            [FromQuery] string? remarks)
        {
            var query = _context.StudentGrades
                .Include(g => g.Student)
                .Include(g => g.Faculty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(studentNumber))
                query = query.Where(g => g.Student != null && g.Student.StudentNumber.Contains(studentNumber));

            if (!string.IsNullOrWhiteSpace(subject))
                query = query.Where(g => g.SubjectCode.Contains(subject) || g.SubjectName.Contains(subject));

            if (!string.IsNullOrWhiteSpace(semester))
                query = query.Where(g => g.Semester == semester);

            if (!string.IsNullOrWhiteSpace(schoolYear))
                query = query.Where(g => g.SchoolYear == schoolYear);

            if (!string.IsNullOrWhiteSpace(remarks))
                query = query.Where(g => g.Remarks == remarks);

            var grades = await query
                .OrderByDescending(g => g.DatePosted)
                .ToListAsync();

            return Ok(grades.Select(MapToDto)); // map in memory - EF cannot translate MapToDto
        }

        // ------------------------------------------------------------------
        // GET: api/grades/{id}
        // Faculty: any record. Student: only their own (else 404 to conceal).
        // ------------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<GradeResponseDto>> GetGradeById(int id)
        {
            var grade = await _context.StudentGrades
                .Include(g => g.Student)
                .Include(g => g.Faculty)
                .FirstOrDefaultAsync(g => g.GradeId == id);

            if (grade == null)
                return NotFound(new { message = "Grade record not found." });

            // Student ownership: a student may only read their own grade.
            if (User.IsInRole("Student"))
            {
                var myNumber = GetStudentNumberFromClaims();
                if (grade.Student == null || grade.Student.StudentNumber != myNumber)
                    return NotFound(new { message = "Grade record not found." }); // conceal existence
            }

            return Ok(MapToDto(grade));
        }

        // ------------------------------------------------------------------
        // GET: api/student/grades   (STUDENT ONLY)
        // StudentId is derived ONLY from the authenticated token - never from
        // query strings, route values, or the request body.
        // ------------------------------------------------------------------
        [HttpGet("/api/student/grades")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<IEnumerable<GradeResponseDto>>> GetMyGrades()
        {
            var myNumber = GetStudentNumberFromClaims();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNumber == myNumber);

            if (student == null)
                return Ok(new List<GradeResponseDto>()); // no matching student record -> empty set

            var myGrades = await _context.StudentGrades
                .Include(g => g.Student)
                .Include(g => g.Faculty)
                .Where(g => g.StudentId == student.StudentId)
                .OrderByDescending(g => g.DatePosted)
                .ToListAsync();

            return Ok(myGrades.Select(MapToDto)); // map in memory
        }

        // ------------------------------------------------------------------
        // POST: api/grades   (FACULTY ONLY)
        // ------------------------------------------------------------------
        [HttpPost]
        [Authorize(Roles = "Faculty")]
        public async Task<ActionResult<GradeResponseDto>> CreateGrade([FromBody] SaveGradeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryValidateGrade(dto.Grade, out int wholeGrade, out string? gradeError))
                return BadRequest(new { message = gradeError });

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNumber == dto.StudentNumber);
            if (student == null)
                return BadRequest(new { message = $"Student with number '{dto.StudentNumber}' was not found." });

            var faculty = await ResolveFacultyAsync();
            if (faculty == null)
                return BadRequest(new { message = "No faculty record is available to attribute this grade to." });

            var newGrade = new StudentGrade
            {
                StudentId = student.StudentId,
                SubjectCode = dto.SubjectCode.Trim(),
                SubjectName = dto.SubjectName.Trim(),
                FacultyId = faculty.FacultyId,
                Grade = wholeGrade,
                Remarks = GradeCalculator.ComputeRemarks(wholeGrade), // SERVER-SIDE ONLY
                Semester = dto.Semester.Trim(),
                SchoolYear = dto.SchoolYear.Trim(),
                DatePosted = DateTime.UtcNow,
                DateModified = null
            };

            _context.StudentGrades.Add(newGrade);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Unique index on (StudentId, SubjectCode, Semester, SchoolYear)
                return Conflict(new
                {
                    message = "A grade for this student, subject, semester, and school year already exists."
                });
            }

            // reload navigation props for a complete DTO
            await _context.Entry(newGrade).Reference(g => g.Student).LoadAsync();
            await _context.Entry(newGrade).Reference(g => g.Faculty).LoadAsync();

            return CreatedAtAction(nameof(GetGradeById), new { id = newGrade.GradeId }, MapToDto(newGrade));
        }

        // ------------------------------------------------------------------
        // PUT: api/grades/{id}   (FACULTY ONLY)
        // ------------------------------------------------------------------
        [HttpPut("{id}")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UpdateGrade(int id, [FromBody] SaveGradeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var gradeRecord = await _context.StudentGrades.FindAsync(id);
            if (gradeRecord == null)
                return NotFound(new { message = "Grade record not found." });

            if (!TryValidateGrade(dto.Grade, out int wholeGrade, out string? gradeError))
                return BadRequest(new { message = gradeError });

            gradeRecord.Grade = wholeGrade;
            gradeRecord.Remarks = GradeCalculator.ComputeRemarks(wholeGrade); // always recalculated
            gradeRecord.SubjectCode = dto.SubjectCode.Trim();
            gradeRecord.SubjectName = dto.SubjectName.Trim();
            gradeRecord.Semester = dto.Semester.Trim();
            gradeRecord.SchoolYear = dto.SchoolYear.Trim();
            gradeRecord.DateModified = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "That change would duplicate an existing subject/semester/school-year for this student."
                });
            }

            await _context.Entry(gradeRecord).Reference(g => g.Student).LoadAsync();
            await _context.Entry(gradeRecord).Reference(g => g.Faculty).LoadAsync();

            return Ok(MapToDto(gradeRecord));
        }

        // ------------------------------------------------------------------
        // DELETE: api/grades/{id}   (FACULTY ONLY)
        // ------------------------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var gradeRecord = await _context.StudentGrades.FindAsync(id);
            if (gradeRecord == null)
                return NotFound(new { message = "Grade record not found." });

            _context.StudentGrades.Remove(gradeRecord);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }

        // ============================ helpers ============================

        private string GetStudentNumberFromClaims()
        {
            // Primary: custom StudentNumber claim. Fallback: the login name.
            return User.FindFirstValue("StudentNumber")
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? string.Empty;
        }

        private async Task<Faculty?> ResolveFacultyAsync()
        {
            var facultyNumber = User.FindFirstValue("FacultyNumber");
            Faculty? faculty = null;

            if (!string.IsNullOrWhiteSpace(facultyNumber))
                faculty = await _context.Faculty.FirstOrDefaultAsync(f => f.FacultyNumber == facultyNumber);

            // Fallback so a grade can still be attributed even if the mapping is missing.
            faculty ??= await _context.Faculty.OrderBy(f => f.FacultyId).FirstOrDefaultAsync();
            return faculty;
        }

        /// <summary>
        /// Whole number 0-100 only. Rejects null/missing and decimals.
        /// </summary>
        private static bool TryValidateGrade(decimal? value, out int grade, out string? error)
        {
            grade = 0;
            error = null;

            if (value == null)
            {
                error = "Grade is required.";
                return false;
            }

            if (value.Value != decimal.Truncate(value.Value))
            {
                error = "Grade must be a whole number. Decimal grades are not allowed.";
                return false;
            }

            if (value.Value < 0 || value.Value > 100)
            {
                error = "Grade must be between 0 and 100.";
                return false;
            }

            grade = (int)value.Value;
            return true;
        }

        private static GradeResponseDto MapToDto(StudentGrade g) => new GradeResponseDto
        {
            GradeId = g.GradeId,
            StudentNumber = g.Student?.StudentNumber ?? g.StudentId.ToString(),
            StudentName = g.Student != null ? $"{g.Student.FirstName} {g.Student.LastName}" : "Unknown Student",
            SubjectCode = g.SubjectCode,
            SubjectName = g.SubjectName,
            FacultyName = g.Faculty != null ? $"{g.Faculty.FirstName} {g.Faculty.LastName}" : "Unknown Faculty",
            Grade = g.Grade,
            Remarks = g.Remarks,
            Semester = g.Semester,
            SchoolYear = g.SchoolYear,
            DatePosted = g.DatePosted,
            DateModified = g.DateModified
        };
    }
}
