using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NjuCsCmsHelper.Server.Controllers
{
    using Models;

    [ApiController]
    [Route("[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly ILogger<ReviewController> logger;
        private readonly ApplicationDbContext dbContext;

        public ReviewController(ILogger<ReviewController> logger, ApplicationDbContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int assignmentId, int reviewerId)
        {
            if (!await IsAssignmentIdExists(assignmentId))
                return NotFound("Assignment ID not exists");
            var infos = await dbContext.Submissions
                .Where(submission => submission.AssignmentId == assignmentId)
                .Include(submission => submission.Student)
                .Where(submission => submission.Student.ReviewerId == reviewerId)
                .Include(submission => submission.NeedCorrection)
                .Include(submission => submission.HasCorrected)
                .AsSplitQuery()
                .ToListAsync();
            var list = infos
                .Select(submission =>
                    new ReviewInfo
                    {
                        StudentId = submission.Student.Id,
                        StudentName = submission.Student.Name,
                        SubmittedAt = submission.SubmittedAt,
                        Grade = submission.Grade,
                        NeedCorrection = submission.NeedCorrection.Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}").ToList(),
                        HasCorrected = submission.HasCorrected.Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}").ToList(),
                        Comment = submission.Comment,
                        Track = submission.Track,
                    }
                )
                .ToList();
            return Ok(list);
        }

        [HttpPut]
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(int assignmentId, [FromBody] List<ReviewInfo> reviewResults)
        {
            if (!await IsAssignmentIdExists(assignmentId))
                return NotFound("Assignment ID not exists");

            foreach (var result in reviewResults)
            {
                if (!await IsStudentIdExists(result.StudentId))
                    return NotFound($"Student ID `{result.StudentId}` not exists");
                var submission = await dbContext.Submissions
                    .SingleOrDefaultAsync(submission => submission.StudentId == result.StudentId && submission.AssignmentId == assignmentId);
                if (submission is null)
                {
                    submission = new Submission
                    {
                        StudentId = result.StudentId,
                        AssignmentId = assignmentId,
                        SubmittedAt = result.SubmittedAt,
                    };
                    await dbContext.Submissions.AddAsync(submission);
                }
                try
                {
                    submission.Grade = result.Grade;
                    submission.Comment = result.Comment;
                    submission.Track = result.Track;
                    await SetMistake(result.NeedCorrection, result.StudentId, submission);
                    await dbContext.Mistakes.Where(m => m.CorrectedInId == submission.Id).ForEachAsync(m => m.CorrectedIn = null);
                    await CorrectMistake(result.HasCorrected, result.StudentId, submission);
                }
                catch (HttpResponseException ex)
                {
                    return new ObjectResult(ex.Value)
                    {
                        StatusCode = ex.Status,
                    };
                }
            }
            await dbContext.SaveChangesAsync();
            return Ok();
        }

        private Task<bool> IsAssignmentIdExists(int assignmentId)
        {
            return dbContext.Assignments.AnyAsync(a => a.Id == assignmentId);
        }
        private Task<bool> IsStudentIdExists(int studentId)
        {
            return dbContext.Students.AnyAsync(s => s.Id == studentId);
        }

        private (int, int) ParseProblemId(string problemName)
        {
            var matchResult = ProblemNameRegex.Match(problemName);
            if (!matchResult.Success)
                throw new FormatException($"Invalid problem name `{problemName}`");
            var assignmentId = int.Parse(matchResult.Groups[1].Value);
            var problemId = int.Parse(matchResult.Groups[2].Value);
            return (assignmentId, problemId);
        }
        private async Task SetMistake(ICollection<string> problemNameList, int studentId, Submission submission)
        {
            var mistakes = await dbContext.Mistakes
                .Where(m => m.MakedInId == submission.Id)
                .ToListAsync();
            var taskList = problemNameList.Select(async problem =>
            {
                var (assignmentId, problemId) = ParseProblemId(problem);
                var mistake = mistakes.SingleOrDefault(mistake => mistake.AssignmentId == assignmentId && mistake.ProblemId == problemId);
                if (mistake != null)
                {
                    mistakes.Remove(mistake);
                    return;
                }
                if (!await IsAssignmentIdExists(assignmentId))
                    throw new HttpResponseException(StatusCodes.Status404NotFound, $"Assignemnt ID `{assignmentId} not exists");
                mistake = new Mistake
                {
                    StudentId = studentId,
                    AssignmentId = assignmentId,
                    ProblemId = problemId,
                    MakedIn = submission,
                };
                await dbContext.Mistakes.AddAsync(mistake);
            });
            await Task.WhenAll(taskList);
            dbContext.Mistakes.RemoveRange(mistakes);
        }
        private async Task CorrectMistake(ICollection<string> problemNameList, int studentId, Submission submission)
        {
            var taskList = problemNameList.Select(async problem =>
            {
                var (assignmentId, problemId) = ParseProblemId(problem);
                var mistake = await dbContext.Mistakes
                    .Where(m => m.StudentId == studentId && m.AssignmentId == assignmentId && m.ProblemId == problemId)
                    .SingleOrDefaultAsync();
                if (mistake == null)
                    throw new HttpResponseException(StatusCodes.Status400BadRequest, $"Student {studentId} didn't make mistake at {problem}");
                if (mistake.CorrectedIn != null)
                    throw new HttpResponseException(StatusCodes.Status400BadRequest, $"Student {studentId} has corrected {problem}");
                mistake.CorrectedIn = submission;
                return mistake;
            });
            await Task.WhenAll(taskList);
        }

        private static readonly Regex ProblemNameRegex = new Regex(@"(\d+)-(\d+)");
    }

    public class ReviewInfo
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public DateTime SubmittedAt { get; set; }
        public Grade Grade { get; set; } = Grade.None;
        public List<string> NeedCorrection { get; set; } = new List<string>();
        public List<string> HasCorrected { get; set; } = new List<string>();
        public string Comment { get; set; } = "";
        public string Track { get; set; } = "";
    }
}
