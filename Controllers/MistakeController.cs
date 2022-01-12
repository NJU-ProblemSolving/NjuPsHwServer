namespace NjuCsCmsHelper.Server.Controllers;

using System.Text;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MistakeController : ControllerBase
{
    private readonly ILogger<MistakeController> logger;
    private readonly ApplicationDbContext dbContext;
    private IMemoryCache cache;

    public MistakeController(ILogger<MistakeController> logger, ApplicationDbContext dbContext, IMemoryCache cache)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var list = await dbContext.Mistakes.Where(m => m.CorrectedIn == null)
                       .Select(m => new { m.StudentId, Mistake = $"{m.AssignmentId}-{m.ProblemId}" })
                       .ToListAsync();
        var res = list.GroupBy(m => m.StudentId)
                      .Select(g => new { StudentId = g.Key, Mistakes = g.Select(m => m.Mistake).ToList() })
                      .ToList();
        return Ok(res);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("{studentId:int}/SendMail")]
    public async Task<IActionResult> SendMail(int studentId)
    {
        var student = await dbContext.Students.SingleOrDefaultAsync(student => student.Id == studentId);
        if (student == null) return NotFound($"Student ID {studentId} not exists");

        if (cache.TryGetValue(studentId, out _)) { return BadRequest("在最近一小时内已发送过反馈邮件，请稍后再试。"); }
        cache.Set(studentId, true, TimeSpan.FromHours(1));

        var submissions = await dbContext.Submissions.Where(submission => submission.StudentId == studentId)
                              .Select(submission => new {
                                  AssignmentId = submission.AssignmentId,
                                  Grade = submission.Grade,
                                  NeedCorrection = submission.NeedCorrection.OrderBy(mistake => mistake.ProblemId)
                                                       .Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}")
                                                       .ToList(),
                                  HasCorrected = submission.HasCorrected.OrderBy(mistake => mistake.ProblemId)
                                                     .Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}")
                                                     .ToList(),
                                  Track = submission.Track,
                              })
                              .ToListAsync();
        var notCorrected =
            await dbContext.Mistakes.Where(mistake => mistake.StudentId == studentId && mistake.CorrectedIn == null)
                .OrderBy(mistake => mistake.AssignmentId)
                .ThenBy(mistake => mistake.ProblemId)
                .Select(mistake => $"{mistake.AssignmentId}-{mistake.ProblemId}")
                .ToListAsync();

        var content = new StringBuilder();
        content.Append($"\n{student.Name}同学你好，你一共提交了{submissions.Count}次作业。\n\n");
        foreach (var submission in submissions)
        {
            content.Append(
                $"在作业1-{submission.AssignmentId}中，你的评分是 {submission.Grade.ToDescriptionString()} 。");
            if (submission.NeedCorrection.Count > 0)
            {
                content.Append($"其中，你需要订正题目");
                content.AppendJoin('、', submission.NeedCorrection);
                content.Append("。");
            }
            else
            {
                content.Append("此次作业你无需进行订正。");
            }
            if (submission.HasCorrected.Count > 0)
            {
                content.Append($"附录中对题目");
                content.AppendJoin('、', submission.HasCorrected);
                content.Append($"的订正已被接受。");
            }
            content.Append("\n");
        }
        content.Append("\n");

        if (notCorrected.Count > 0)
        {
            content.Append("目前，你还没有订正题目");
            content.AppendJoin('、', notCorrected);
            content.Append("。请及时进行订正。\n");
        }
        else
        {
            content.Append("恭喜，你目前没有需要订正的题目。\n");
        }

        content.Append("\n");
        content.Append("此邮件为脚本自动发送，请勿直接回复。");
        content.Append(
            $"在需要时你也可以访问 https://host.lihan.website:32109/api/Mistake/{student.Id}/SendMail 获取该邮件的最新版本。\n");
        content.Append("如果你对某次提交有疑问，请及时联系李晗助教\n");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("21级问题求解助教团队", "191870085@smail.nju.edu.cn"));
        message.To.Add(new MailboxAddress(student.Name, $"{student.Id}@smail.nju.edu.cn"));
        message.Subject = "【问题求解】作业情况反馈";
        message.Body = new TextPart() { Text = content.ToString() };

        try
        {
            using (var client = new SmtpClient())
            {
                // client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                // await client.ConnectAsync("mxbiz1.qq.com", options: MailKit.Security.SecureSocketOptions.None);
                await client.ConnectAsync("smtp.exmail.qq.com");
                await client.AuthenticateAsync("191870085@smail.nju.edu.cn", "!8vz$$%f0eooickj");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return Ok("发送邮件时出现错误，请联系李晗助教。");
        }
        return Ok("查询成功，请到学校邮箱中查看详细反馈。");
    }
}