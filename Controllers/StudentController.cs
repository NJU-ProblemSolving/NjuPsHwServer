namespace NjuCsCmsHelper.Server.Controllers;

using AutoMapper;
using Datas;
using Services;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StudentController : AppControllerBase<StudentController>
{
    public StudentController(IServiceProvider provider) : base(provider) { }

    /// <summary>新建学生用户</summary>
    [HttpPost]
    [Authorize("Admin")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStudent([FromBody] StudentDto studentDto)
    {
        if (await dbContext.Students.AnyAsync(s => s.Id == studentDto.Id))
            return Conflict("Studend ID already exists");

        var student = dtoMapper.Map<StudentDto, Student>(studentDto);
        await dbContext.Students.AddAsync(student);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetStudentInfo), new { studentId = student.Id }, null);
    }

    /// <summary>获取学生信息</summary>
    [HttpGet]
    [Authorize("Admin")]
    [Route("{studentId:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentInfo(int studentId)
    {
        var authorizeResult = await authorizationService.AuthorizeAsync(User, studentId, OwnerOrAdminRequirement.Instance);
        if (!authorizeResult.Succeeded) return Forbid();

        var student = await dbContext.Students.Where(student => student.Id == studentId).SingleOrDefaultAsync();
        if (student == null) return NotFound("Student ID not found");

        return Ok(dtoMapper.Map<Student, StudentDto>(student));
    }

    /// <summary>删除学生用户</summary>
    [HttpDelete]
    [Authorize("Admin")]
    [Route("{studentId:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudent(int studentId)
    {
        var student = await dbContext.Students.Where(student => student.Id == studentId).SingleOrDefaultAsync();
        if (student == null) return NotFound("Student ID not found");

        dbContext.Students.Remove(student);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>重置 Token 并发送邮件</summary>
    [HttpPost]
    [AllowAnonymous]
    [Route("ResetToken")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetToken([Required] int studentId)
    {
        var student = await dbContext.Students.Where(student => student.Id == studentId).SingleOrDefaultAsync();
        if (student == null) return NotFound("Student ID not found");

        var token = new Token { StudentId = studentId, Id = null!, IsAdmin = false };
        var random = new Random();
        var base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < 10; i++)
            sb.Append(base62[random.Next(62)]);
        token.Id = sb.ToString();

        try
        {
            await mailingService.SendToken(studentId, token.Id);
        }
        catch (HttpResponseException ex)
        {
            return new ObjectResult(ex.Value)
            {
                StatusCode = ex.Status,
            };
        }

        var tokenget = await dbContext.Tokens.FirstOrDefaultAsync(token => token.StudentId == studentId);
        if(tokenget != null) {
            dbContext.Tokens.Remove(tokenget);
            await dbContext.SaveChangesAsync();
        }
        dbContext.Tokens.Add(token);
        await dbContext.SaveChangesAsync();
        return Ok();
    }
}
